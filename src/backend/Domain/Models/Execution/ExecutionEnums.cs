namespace Domain.Models.Execution;

public enum ProcessingStatus
{
    Pending = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum ExecutionVerdict
{
    Succeeded = 0,
    Accepted = 1,
    WrongAnswer = 2,
    TimeLimitExceeded = 3,
    MemoryLimitExceeded = 4,
    OutputLimitExceeded = 5,
    CompilationError = 6,
    RuntimeError = 7
}
