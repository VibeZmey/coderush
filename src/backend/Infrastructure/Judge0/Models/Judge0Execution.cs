using Domain.Models.Common;
using Domain.Models.Execution;

namespace Infrastructure.Judge0.Models;

/// <summary>Persistent dispatch/polling state for one execution attempt.</summary>
public sealed class Judge0Execution : Entity
{
    public Guid ExecutionJobId { get; set; }
    /// <summary>Null until Judge0 accepts the request. Do not blindly resend ambiguous HTTP failures.</summary>
    public string? SubmissionToken { get; set; }
    public int? LastStatusId { get; set; }
    public int DispatchAttempts { get; set; }
    public DateTimeOffset? LastPolledAt { get; set; }
    public DateTimeOffset? NextPollAt { get; set; }
    public DateTimeOffset? ProcessingDeadlineAt { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public string? LastError { get; set; }
    /// <summary>Persistence must use this as a concurrency token when claiming work.</summary>
    public long Revision { get; set; }

    public ExecutionJob ExecutionJob { get; set; } = null!;
}
