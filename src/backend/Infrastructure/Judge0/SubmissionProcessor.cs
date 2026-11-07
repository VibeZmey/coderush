using Domain.Models.Execution;
using Domain.Models.Matches;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Judge0;

public sealed class SubmissionProcessor(AppDbContext db)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Lock only solutions with an unconsumed terminal test. No HTTP is performed under this lock.
        var candidates = await db.Set<Submission>().FromSqlRaw("""
            SELECT s.* FROM "Submissions" s
            WHERE s."Status" IN (0, 1, 2) AND EXISTS (
                SELECT 1 FROM "SubmissionTestResults" t
                JOIN "ExecutionJobs" j ON j."Id" = t."ExecutionJobId"
                WHERE t."SubmissionId" = s."Id" AND t."Verdict" IS NULL AND j."Status" IN (3, 4, 5))
            ORDER BY s."SubmittedAt" LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);
        var submission = candidates.SingleOrDefault();
        if (submission is null) return false;

        await db.Entry(submission).Collection(x => x.TestResults).Query().Include(x => x.ExecutionJob).LoadAsync(ct);
        await db.Entry(submission).Reference(x => x.MatchProblem).LoadAsync(ct);
        var problem = submission.MatchProblem;
        var isDuel = submission.DuelPlayerId is not null;
        if (isDuel)
            await DuelService.LockAsync(db, ct);
        await db.Entry(problem).Reference(x => x.ProblemVersion).Query().Include(x => x.TestCases).LoadAsync(ct);
        var tests = problem.ProblemVersion.TestCases.OrderBy(x => x.Position).ToArray();
        var current = submission.TestResults.Single(x => x.Verdict is null);
        var job = current.ExecutionJob;
        if (job.Status != ProcessingStatus.Completed || job.Result is null)
        {
            if (isDuel && job.RetryOfJobId is null)
            {
                var test = tests.Single(x => x.Id == current.ProblemTestCaseId);
                current.ExecutionJob = SubmissionService.AddJob(db, submission, test, problem.ProblemVersion, job.Id);
                current.ExecutionJobId = current.ExecutionJob.Id;
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }

            submission.Status = ProcessingStatus.Failed;
            submission.FailureCode = job.FailureCode ?? "Judge0Error";
        }
        else
        {
            current.Verdict = job.Result.Verdict;
            if (!isDuel && current.Verdict != ExecutionVerdict.Accepted)
            {
                submission.Status = ProcessingStatus.Completed;
                submission.Verdict = current.Verdict;
            }
            else if (submission.TestResults.Count == tests.Length)
            {
                submission.Status = ProcessingStatus.Completed;
                var byTest = submission.TestResults.ToDictionary(x => x.ProblemTestCaseId);
                submission.Verdict = tests.Select(x => byTest[x.Id].Verdict)
                    .FirstOrDefault(x => x != ExecutionVerdict.Accepted) ?? ExecutionVerdict.Accepted;
            }
            else
            {
                var next = tests[submission.TestResults.Count];
                SubmissionService.AddTest(db, submission, next, problem.ProblemVersion);
            }
        }

        if (submission.Status is ProcessingStatus.Completed or ProcessingStatus.Failed)
        {
            submission.CompletedAt = DateTimeOffset.UtcNow;
            var matchQuery = db.Entry(problem).Reference(x => x.Match);
            if (isDuel)
            {
                await matchQuery.LoadAsync(ct);
                await db.SaveChangesAsync(ct);
                await db.Entry(problem).Collection(x => x.Submissions).Query()
                    .Include(x => x.TestResults).LoadAsync(ct);
                ((DuelMatch)problem.Match).Refresh(DateTimeOffset.UtcNow);
            }
            else
            {
                await matchQuery.Query().Include(x => x.Problems).ThenInclude(x => x.Submissions).LoadAsync(ct);
                if (problem.Match is TrainingMatch training)
                    training.Refresh(DateTimeOffset.UtcNow);
            }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}

