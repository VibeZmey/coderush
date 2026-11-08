using System.Security.Claims;
using Api.Contracts.Matches;
using Api.Validation;
using Application.Features.Matches.TrainingSession;
using Domain.Models.Execution;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class Practice
{
    public static void MapPracticeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/practice").WithTags("Practice");
        group.MapGet("/", CatalogAsync).AllowAnonymous();
        group.MapGet("/{id:guid}", PreviewAsync).AllowAnonymous();
        group.MapPost("/{id:guid}/open", OpenAsync).RequireAuthorization();
        group.MapPut("/drafts/{id:guid}", SaveDraftAsync).RequireAuthorization()
            .AddEndpointFilter<RequestValidationFilter<SaveTrainingDraftRequest>>();
    }

    private static IQueryable<ProblemVersion> Published(AppDbContext db) =>
        db.ProblemVersions.Where(v => v.Problem.IsPractice && !v.Problem.IsArchived &&
            v.Status == PublicationStatus.Published && v.Languages.Any(l => l.ProgrammingLanguage.IsEnabled) &&
            !v.Problem.Versions.Any(newer => newer.Status == PublicationStatus.Published && newer.VersionNumber > v.VersionNumber));

    private static IQueryable<Submission> Judged(AppDbContext db, string? userId) =>
        db.Set<Submission>().Where(s => userId != null && s.MatchProblem.Match is PracticeMatch &&
            s.MatchProblem.Match.UserId == userId && s.Status == ProcessingStatus.Completed && s.Verdict != null);

    private static IQueryable<Submission> Accepted(AppDbContext db, string? userId) =>
        Judged(db, userId).Where(s => s.Verdict == ExecutionVerdict.Accepted);

    internal static async Task<PracticeStats> StatsAsync(AppDbContext db, string userId, CancellationToken ct)
    {
        var judged = Judged(db, userId);
        var counts = await judged.GroupBy(s => s.Verdict == ExecutionVerdict.Accepted)
            .Select(g => new { Accepted = g.Key, Count = g.Count() }).ToArrayAsync(ct);
        // A task counts once, at the difficulty of the version first solved.
        var solved = await Accepted(db, userId).GroupBy(s => s.MatchProblem.ProblemVersion.ProblemId)
            .Select(g => g.OrderBy(s => s.SubmittedAt).ThenBy(s => s.Id)
                .Select(s => s.MatchProblem.ProblemVersion.Difficulty).First()).ToArrayAsync(ct);
        var total = counts.Sum(x => x.Count);
        var accepted = counts.Where(x => x.Accepted).Sum(x => x.Count);
        return new(solved.Length, solved.Count(d => d == ProblemDifficulty.Easy),
            solved.Count(d => d == ProblemDifficulty.Medium), solved.Count(d => d == ProblemDifficulty.Hard),
            accepted, total, total == 0 ? null : Math.Round(100.0 * accepted / total, 1));
    }

    internal sealed record PracticeStats(int Solved, int Easy, int Medium, int Hard,
        int AcceptedSubmissions, int JudgedSubmissions, double? AcceptancePercent);

    private static Task<ProblemVersion?> LoadPublishedAsync(AppDbContext db, Guid id, CancellationToken ct) =>
        Published(db).Where(v => v.ProblemId == id)
            .Include(v => v.Languages).ThenInclude(l => l.ProgrammingLanguage)
            .Include(v => v.TestCases.Where(t => t.IsSample)).AsSplitQuery().SingleOrDefaultAsync(ct);

    private static async Task<IResult> PreviewAsync(Guid id, AppDbContext db, CancellationToken ct)
    {
        var version = await LoadPublishedAsync(db, id, ct);
        return version is null ? Results.NotFound() :
            Workspace(new MatchProblem { Id = version.Id, ProblemVersion = version }, version, false, null);
    }

    private static IResult Workspace(MatchProblem assignment, ProblemVersion version, bool solved, Guid? latestSubmissionId) =>
        Results.Ok(new
        {
            Problem = TrainingMatchViews.CreateProblem(assignment),
            Difficulty = version.Difficulty.ToString(), version.Topics, Solved = solved, LatestSubmissionId = latestSubmissionId
        });

    private static async Task<IResult> CatalogAsync(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var accepted = Accepted(db, user.FindFirstValue(ClaimTypes.NameIdentifier));
        var items = await Published(db).AsNoTracking().OrderBy(v => v.Title).ThenBy(v => v.ProblemId)
            .Select(v => new
            {
                Id = v.ProblemId, v.Problem.Key, v.Title, Difficulty = v.Difficulty.ToString(), v.Topics,
                Solved = accepted.Any(s => s.MatchProblem.ProblemVersion.ProblemId == v.ProblemId)
            }).ToArrayAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> OpenAsync(Guid id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(user);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize workspace creation for the same player and task across tabs and API instances.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"practice:{userId}:{id:D}"}, 0))", ct);
        var version = await LoadPublishedAsync(db, id, ct);
        if (version is null) return Results.NotFound();

        var assignment = await db.MatchProblems
            .Include(p => p.Drafts)
            .SingleOrDefaultAsync(p => p.Match is PracticeMatch && p.Match.UserId == userId && p.ProblemVersionId == version.Id, ct);
        if (assignment is null)
        {
            var now = DateTimeOffset.UtcNow;
            var match = new PracticeMatch { UserId = userId };
            match.Start(now);
            assignment = new MatchProblem
            {
                Match = match, ProblemVersion = version, Position = 1, Status = MatchProblemStatus.Active, StartedAt = now
            };
            db.MatchProblems.Add(assignment);
            await db.SaveChangesAsync(ct);
        }

        var latestSubmissionId = await db.Set<Submission>().Where(s => s.MatchProblemId == assignment.Id)
            .OrderByDescending(s => s.SubmittedAt).ThenByDescending(s => s.Id).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        var solved = await Accepted(db, userId).AnyAsync(s => s.MatchProblem.ProblemVersion.ProblemId == id, ct);
        await transaction.CommitAsync(ct);
        return Workspace(assignment, version, solved, latestSubmissionId);
    }

    private static async Task<IResult> SaveDraftAsync(Guid id, SaveTrainingDraftRequest request,
        ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = UserId(user);
        if (id != request.MatchProblemId || !await db.MatchProblems.AnyAsync(
            p => p.Id == id && p.Match is PracticeMatch && p.Match.UserId == userId, ct))
            return Results.NotFound();
        if (!await db.ProblemLanguages.AnyAsync(l => l.ProgrammingLanguageId == request.ProgrammingLanguageId &&
            l.ProgrammingLanguage.IsEnabled && db.MatchProblems.Any(p => p.Id == id && p.ProblemVersionId == l.ProblemVersionId), ct))
            return Results.BadRequest(new { message = "This language is not available for the match problem." });

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "CodeDrafts" ("Id", "MatchProblemId", "ProgrammingLanguageId", "SourceCode", "CustomInput", "CreatedAt", "UpdatedAt")
            VALUES ({Guid.NewGuid()}, {id}, {request.ProgrammingLanguageId}, {request.SourceCode}, {request.StandardInput}, now(), now())
            ON CONFLICT ("MatchProblemId", "ProgrammingLanguageId") DO UPDATE SET
                "SourceCode" = EXCLUDED."SourceCode", "CustomInput" = EXCLUDED."CustomInput", "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, ct);
        return Results.NoContent();
    }

    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
