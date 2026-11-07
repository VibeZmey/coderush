using Application.Features.Code.SubmitCode;
using Domain.Models.Execution;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Infrastructure.Judge0.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Judge0;

public sealed class SubmissionService(AppDbContext db) : ISubmissionService
{
    public async Task<SubmitCodeResult> SubmitAsync(SubmitCodeCommand command, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var isDuel = await db.MatchProblems.AnyAsync(x => x.Id == command.MatchProblemId && x.Match is DuelMatch, ct);
        if (isDuel)
            await DuelService.LockAsync(db, ct);
        // Serialize acceptance for this assignment; each player can have one check in flight.
        var locked = await db.MatchProblems.FromSqlInterpolated(
            $"SELECT * FROM \"MatchProblems\" WHERE \"Id\" = {command.MatchProblemId} FOR UPDATE").ToListAsync(ct);
        var problem = locked.SingleOrDefault();
        if (problem is null) return new(SubmitCodeOutcome.NotFound);
        await db.Entry(problem).Reference(x => x.Match).LoadAsync(ct);
        var duel = problem.Match as DuelMatch;
        if (!(duel?.HasPlayer(command.UserId) ?? (problem.Match.UserId == command.UserId)))
            return new(SubmitCodeOutcome.NotFound);
        var now = DateTimeOffset.UtcNow;
        if (problem.Match.Status != MatchStatus.InProgress || problem.Match.FinishedAt is not null ||
            problem.Status != MatchProblemStatus.Active || problem.ClosedAt is not null ||
            problem.StartedAt is null || problem.StartedAt > now || problem.SubmissionDeadlineAt is null || problem.SubmissionDeadlineAt <= now)
            return new(SubmitCodeOutcome.InvalidState);
        var version = await db.ProblemVersions.Include(x => x.Languages).ThenInclude(x => x.ProgrammingLanguage)
            .Include(x => x.TestCases).AsSplitQuery().SingleAsync(x => x.Id == problem.ProblemVersionId, ct);
        problem.ProblemVersion = version;
        if (!version.Languages.Any(x => x.ProgrammingLanguageId == command.ProgrammingLanguageId && x.ProgrammingLanguage.IsEnabled))
            return new(SubmitCodeOutcome.LanguageUnavailable);
        if (await db.Set<Submission>().AnyAsync(x => x.MatchProblemId == problem.Id && (x.DuelPlayerId == null || x.DuelPlayerId == command.UserId) &&
            (x.Status == ProcessingStatus.Pending || x.Status == ProcessingStatus.Queued || x.Status == ProcessingStatus.Running), ct))
            return new(SubmitCodeOutcome.AlreadyChecking);
        if (version.TestCases.Count == 0) return new(SubmitCodeOutcome.InvalidState);

        var submission = new Submission
        {
            MatchProblemId = problem.Id, MatchProblem = problem, ProgrammingLanguageId = command.ProgrammingLanguageId,
            DuelPlayerId = duel is null ? null : command.UserId, SourceCode = command.SourceCode, SubmittedAt = now, Status = ProcessingStatus.Running
        };
        db.Set<Submission>().Add(submission);
        AddTest(db, submission, version.TestCases.OrderBy(x => x.Position).First(), version);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(SubmitCodeOutcome.Accepted, ToView(submission));
    }

    internal static void AddTest(AppDbContext db, Submission submission, ProblemTestCase test, ProblemVersion version)
    {
        var job = AddJob(db, submission, test, version);
        var result = new SubmissionTestResult
        {
            SubmissionId = submission.Id, ProblemTestCaseId = test.Id, ProblemTestCase = test,
            ExecutionJobId = job.Id, ExecutionJob = job
        };
        submission.TestResults.Add(result);
        db.Set<SubmissionTestResult>().Add(result);
    }

    internal static ExecutionJob AddJob(AppDbContext db, Submission submission, ProblemTestCase test,
        ProblemVersion version, Guid? retryOf = null)
    {
        var job = new ExecutionJob
        {
            ProgrammingLanguageId = submission.ProgrammingLanguageId, SourceCode = submission.SourceCode, RetryOfJobId = retryOf,
            StandardInput = test.Input, CpuTimeLimitSeconds = version.CpuTimeLimitSeconds,
            WallTimeLimitSeconds = version.WallTimeLimitSeconds, MemoryLimitKilobytes = version.MemoryLimitKilobytes,
            OutputLimitKilobytes = version.OutputLimitKilobytes
        };
        db.Set<Judge0Execution>().Add(new Judge0Execution { ExecutionJobId = job.Id, ExecutionJob = job });
        return job;
    }

    public async Task<SubmissionView?> GetAsync(Guid id, string userId, CancellationToken ct)
    {
        var submission = await db.Set<Submission>().AsNoTracking()
            .Where(x => x.Id == id && (x.DuelPlayerId ?? x.MatchProblem.Match.UserId) == userId)
            .Include(x => x.MatchProblem).ThenInclude(x => x.ProblemVersion).ThenInclude(x => x.TestCases)
            .Include(x => x.TestResults).ThenInclude(x => x.ExecutionJob)
            .Include(x => x.TestResults).ThenInclude(x => x.ProblemTestCase)
            .AsSplitQuery().SingleOrDefaultAsync(ct);
        return submission is null ? null : ToView(submission);
    }

    private static SubmissionView ToView(Submission submission)
    {
        var tests = submission.MatchProblem.ProblemVersion.TestCases.OrderBy(x => x.Position).ToArray();
        var failed = submission.TestResults.OrderBy(x => x.ProblemTestCase.Position)
            .FirstOrDefault(x => x.Verdict is not null && x.Verdict != ExecutionVerdict.Accepted ||
            x.ExecutionJob.Status is ProcessingStatus.Failed or ProcessingStatus.Cancelled);
        var result = failed?.ExecutionJob.Result;
        return new(submission.Id, submission.Status.ToString(), submission.Verdict?.ToString(),
            submission.TestResults.Count(x => x.Verdict == ExecutionVerdict.Accepted), tests.Length,
            failed is null ? null : Array.FindIndex(tests, x => x.Id == failed.ProblemTestCaseId) + 1,
            submission.FailureCode, result?.CompilationOutput, result?.ExitCode, result?.ExitSignal,
            failed?.ProblemTestCase.IsSample == true ? new(failed.ProblemTestCase.Input, failed.ProblemTestCase.ExpectedOutput,
                result?.StandardOutput, result?.StandardError) : null, submission.SubmittedAt);
    }
}

