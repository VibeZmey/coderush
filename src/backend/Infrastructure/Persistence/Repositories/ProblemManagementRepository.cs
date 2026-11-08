using Application.Features.Problems.CreateProblem;
using Application.Features.Problems.ManageProblems;
using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Application.Common;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProblemManagementRepository(AppDbContext context) : IProblemManagementRepository
{
    private IQueryable<ProblemVersion> Latest() => context.ProblemVersions.AsNoTracking()
        .Where(v => !v.Problem.IsArchived && v.VersionNumber == v.Problem.Versions.Max(x => x.VersionNumber));

    public async Task<ProblemPage> SearchAsync(ProblemSearch search, CancellationToken cancellationToken)
    {
        var query = Latest();
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var escaped = search.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(v => EF.Functions.ILike(v.Title, pattern, "\\") || EF.Functions.ILike(v.Problem.Key, pattern, "\\"));
        }
        if (search.IsPractice is { } isPractice) query = query.Where(v => v.Problem.IsPractice == isPractice);
        if (search.Difficulty is { } difficulty) query = query.Where(v => v.Difficulty == difficulty);
        if (search.Status is { } status) query = query.Where(v => v.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var ordered = search.Sort switch
        {
            "oldest" => query.OrderBy(v => v.CreatedAt),
            "title" => query.OrderBy(v => v.Title),
            "title-desc" => query.OrderByDescending(v => v.Title),
            "difficulty" => query.OrderBy(v => v.Difficulty),
            "tests" => query.OrderByDescending(v => v.TestCases.Count),
            _ => query.OrderByDescending(v => v.CreatedAt)
        };
        var items = await ordered.ThenBy(v => v.Id).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(v => new ProblemSummary(v.ProblemId, v.Problem.Key, v.Id, v.VersionNumber, v.Title,
                v.Difficulty, v.Status, v.TestCases.Count, v.Languages.Count, v.CreatedAt, v.Problem.IsPractice, v.Topics))
            .ToArrayAsync(cancellationToken);
        return new(items, total, search.Page, search.PageSize);
    }

    public async Task<ProblemDetails?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var version = await Latest().Where(v => v.ProblemId == id).Include(v => v.Problem)
            .Include(v => v.Languages).Include(v => v.TestCases).AsSplitQuery().SingleOrDefaultAsync(cancellationToken);
        if (version is null) return null;
        return new(new ProblemSummary(id, version.Problem.Key, version.Id, version.VersionNumber, version.Title,
                version.Difficulty, version.Status, version.TestCases.Count, version.Languages.Count, version.CreatedAt, version.Problem.IsPractice, version.Topics),
            version.DescriptionMarkdown, version.InputDescriptionMarkdown, version.OutputDescriptionMarkdown,
            version.ConstraintsMarkdown, version.ExplanationMarkdown, version.CpuTimeLimitSeconds,
            version.WallTimeLimitSeconds, version.MemoryLimitKilobytes, version.OutputLimitKilobytes,
            version.Languages.OrderBy(l => l.ProgrammingLanguageId).Select(l => new ProblemLanguageSettings(l.ProgrammingLanguageId,
                DefaultCodeTemplates.Resolve(l.ProgrammingLanguageId, l.Template))).ToArray(),
            version.TestCases.OrderBy(t => t.Position).Select(t => new ProblemTestCaseData(t.Input, t.ExpectedOutput, t.IsSample)).ToArray());
    }

    public Task<Problem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        context.Problems.Where(p => p.Id == id && !p.IsArchived)
            .Include(p => p.Versions.OrderByDescending(v => v.VersionNumber).Take(1)).SingleOrDefaultAsync(cancellationToken);

    public async Task<ProblemWriteOutcome> SaveVersionAsync(Problem problem, ProblemVersion version, CancellationToken cancellationToken)
    {
        context.ProblemVersions.Add(version);
        return await SaveAsync(cancellationToken);
    }

    public async Task<ProblemWriteOutcome> ImportAsync(IReadOnlyList<Problem> problems, CancellationToken cancellationToken)
    {
        context.Problems.AddRange(problems);
        // One SaveChanges transaction: a conflicting item rolls back the entire import.
        return await SaveAsync(cancellationToken);
    }

    private async Task<ProblemWriteOutcome> SaveAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); return ProblemWriteOutcome.Saved; }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return pg.ConstraintName == "IX_Problems_Key" ? ProblemWriteOutcome.DuplicateKey : ProblemWriteOutcome.Conflict;
        }
    }
}
