using Application.Common.Interfaces;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(AppDbContext context) : IMatchRepository
{
    public async Task<Match?> GetByIdAsync(Guid id)
    {
        return await WithDetails().SingleOrDefaultAsync(match => match.Id == id);
    }
    public async Task AddAsync(Match match)
    {
        context.Matches.Add(match);
        await context.SaveChangesAsync();
    }

    public Task UpdateAsync(Match match)
    {
        foreach (var problem in match.Problems)
        {
            if (context.Entry(problem).State == EntityState.Detached)
                context.MatchProblems.Add(problem);
            foreach (var draft in problem.Drafts)
                if (context.Entry(draft).State == EntityState.Detached)
                    context.CodeDrafts.Add(draft);
        }
        return context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Guid>> SelectProblemVersionsAsync(ProblemDifficulty difficulty, int count) =>
        await context.ProblemVersions.AsNoTracking()
            .Where(version => version.Status == PublicationStatus.Published && !version.Problem.IsArchived &&
                version.Difficulty == difficulty && version.Languages.Any(language => language.ProgrammingLanguage.IsEnabled) &&
                !context.ProblemVersions.Any(newer => newer.ProblemId == version.ProblemId &&
                    newer.Status == PublicationStatus.Published && newer.VersionNumber > version.VersionNumber))
            .OrderBy(_ => EF.Functions.Random()).Take(count).Select(version => version.Id).ToArrayAsync();

    public async Task<IReadOnlyList<Match>> ListByUserAsync(string userId) =>
        await WithDetails().OfType<TrainingMatch>().Where(match => match.UserId == userId)
            .OrderByDescending(match => match.CreatedAt).Take(50).ToListAsync();

    private IQueryable<Match> WithDetails() => context.Matches
        .Include(match => match.Problems).ThenInclude(problem => problem.ProblemVersion)
            .ThenInclude(version => version.Languages).ThenInclude(language => language.ProgrammingLanguage)
        .Include(match => match.Problems).ThenInclude(problem => problem.ProblemVersion)
            .ThenInclude(version => version.TestCases.Where(test => test.IsSample))
        .Include(match => match.Problems).ThenInclude(problem => problem.Drafts)
        .Include(match => match.Problems).ThenInclude(problem => problem.CodeRuns)
        .Include(match => match.Problems).ThenInclude(problem => problem.Submissions)
        .AsSplitQuery();
}

