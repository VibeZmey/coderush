using Application.Common.Interfaces;
using Domain.Models.Execution;
using Domain.Models.Matches;
using Microsoft.EntityFrameworkCore;
using Application.Features.Code.RunCode;
using Infrastructure.Judge0.Models;

namespace Infrastructure.Persistence.Repositories;

public sealed class CodeRunRepository(AppDbContext context) : ICodeRunRepository, ICodeRunReader
{
    public Task<CodeRunView?> GetAsync(Guid id, string userId, CancellationToken cancellationToken) =>
        context.CodeRuns.AsNoTracking().Where(run => run.Id == id && (run.DuelPlayerId ?? run.MatchProblem.Match.UserId) == userId)
            .Select(run => new CodeRunView(run.Id, run.ExecutionJob.Status, run.CreatedAt,
                run.ExecutionJob.CompletedAt, run.ExecutionJob.FailureCode, run.ExecutionJob.Result))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<MatchProblem?> GetMatchProblemAsync(Guid id) => context.MatchProblems
        .AsNoTracking()
        .Include(problem => problem.Match)
        .Include(problem => problem.ProblemVersion)
            .ThenInclude(version => version.Languages)
                .ThenInclude(language => language.ProgrammingLanguage)
        .SingleOrDefaultAsync(problem => problem.Id == id);

    public async Task AddAsync(CodeRun run)
    {
        // EF inserts the new run and its new job in the same SaveChanges transaction.
        context.CodeRuns.Add(run);
        context.Set<Judge0Execution>().Add(new Judge0Execution { ExecutionJobId = run.ExecutionJobId, ExecutionJob = run.ExecutionJob });
        await context.SaveChangesAsync();
    }
}
