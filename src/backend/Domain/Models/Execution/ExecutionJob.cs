using Domain.Models.Common;
using Domain.Models.Problems;

namespace Domain.Models.Execution;

/// <summary>One execution. A retry creates a new job linked through RetryOfJobId.</summary>
public sealed class ExecutionJob : Entity
{
    public int ProgrammingLanguageId { get; set; }
    public required string SourceCode { get; set; }
    public required string StandardInput { get; set; }
    // Snapshot limits so retries do not depend on subsequently edited settings.
    public double CpuTimeLimitSeconds { get; set; } = 2;
    public double WallTimeLimitSeconds { get; set; } = 10;
    public int MemoryLimitKilobytes { get; set; } = 128000;
    public int OutputLimitKilobytes { get; set; } = 1024;
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public Guid? RetryOfJobId { get; set; }
    public DateTimeOffset? QueuedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public ExecutionResult? Result { get; set; }

    public ProgrammingLanguage ProgrammingLanguage { get; set; } = null!;
    public ExecutionJob? RetryOfJob { get; set; }
}
