using System.Text.Json;
using Domain.Models.Execution;
using Infrastructure.Judge0.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Judge0;

public sealed class CodeExecutionProcessor(AppDbContext db, Judge0Client client, IOptions<Judge0Options> options,
    ILogger<CodeExecutionProcessor> logger)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var candidate = await db.Set<Judge0Execution>().AsNoTracking()
            .Where(x => (x.ExecutionJob.Status == ProcessingStatus.Pending || x.ExecutionJob.Status == ProcessingStatus.Queued ||
                         x.ExecutionJob.Status == ProcessingStatus.Running) &&
                        (x.NextPollAt == null || x.NextPollAt <= now) && (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= now))
            .OrderBy(x => x.NextPollAt ?? x.CreatedAt).FirstOrDefaultAsync(ct);
        if (candidate is null) return false;

        var owner = Guid.NewGuid().ToString();
        // Compare-and-swap claim supports multiple API instances without duplicate dispatch.
        var claimed = await db.Set<Judge0Execution>()
            .Where(x => x.Id == candidate.Id && x.Revision == candidate.Revision && (x.LeaseExpiresAt == null || x.LeaseExpiresAt <= now))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseOwner, owner)
                .SetProperty(x => x.LeaseExpiresAt, now.AddSeconds(60)).SetProperty(x => x.Revision, x => x.Revision + 1), ct);
        if (claimed == 0) return true;

        var execution = await db.Set<Judge0Execution>().Include(x => x.ExecutionJob).SingleAsync(x => x.Id == candidate.Id, ct);
        var job = execution.ExecutionJob;
        var test = await db.Set<SubmissionTestResult>().Where(x => x.ExecutionJobId == job.Id)
            .Select(x => new { x.ProblemTestCase.ExpectedOutput }).SingleOrDefaultAsync(ct);
        execution.ProcessingDeadlineAt ??= now.AddSeconds(options.Value.ProcessingTimeoutSeconds);
        try
        {
            if (execution.ProcessingDeadlineAt <= now)
                Fail(job, "ExecutionTimeout");
            else if (execution.SubmissionToken is null)
            {
                if (execution.DispatchAttempts > 0)
                    Fail(job, "DispatchUncertain");
                else
                {
                    // Persist intent before HTTP. An interrupted/ambiguous POST must never be replayed.
                    execution.DispatchAttempts++;
                    execution.Revision++;
                    await db.SaveChangesAsync(ct);
                    execution.SubmissionToken = await client.DispatchAsync(job, ct, test?.ExpectedOutput);
                    job.Status = ProcessingStatus.Queued;
                    job.QueuedAt = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                var response = await client.PollAsync(execution.SubmissionToken, ct);
                execution.LastStatusId = response.Status?.Id;
                execution.LastPolledAt = DateTimeOffset.UtcNow;
                switch (response.Status?.Id)
                {
                    case 1: job.Status = ProcessingStatus.Queued; break;
                    case 2:
                        job.Status = ProcessingStatus.Running;
                        job.StartedAt ??= DateTimeOffset.UtcNow;
                        break;
                    case >= 3 and <= 12:
                        job.Result = response.ToResult(test is not null);
                        job.Status = ProcessingStatus.Completed;
                        job.CompletedAt = DateTimeOffset.UtcNow;
                        break;
                    default: Fail(job, "Judge0Error"); break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or FormatException)
        {
            // GET is safe to retry until the processing deadline. POST is not.
            if (execution.SubmissionToken is null)
            {
                var failureCode = ex is HttpRequestException { StatusCode: { } status } && (int)status is >= 400 and < 500
                    ? "Judge0Rejected" : "DispatchUncertain";
                Fail(job, failureCode);
            }
            logger.LogWarning(ex, "Judge0 request failed for execution job {ExecutionJobId}.", job.Id);
            execution.LastError = ex.Message[..Math.Min(ex.Message.Length, 4096)];
        }

        execution.NextPollAt = DateTimeOffset.UtcNow.AddSeconds(1);
        execution.LeaseOwner = null;
        execution.LeaseExpiresAt = null;
        execution.Revision++;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void Fail(ExecutionJob job, string code)
    {
        job.Status = ProcessingStatus.Failed;
        job.FailureCode = code;
        job.CompletedAt = DateTimeOffset.UtcNow;
    }
}
