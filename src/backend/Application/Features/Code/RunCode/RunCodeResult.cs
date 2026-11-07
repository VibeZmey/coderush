using Domain.Models.Execution;

namespace Application.Features.Code.RunCode;

public enum RunCodeOutcome
{
    Accepted,
    LanguageUnavailable,
    NotFound,
    InvalidState
}

public sealed record AcceptedCodeRun(Guid Id, ProcessingStatus Status, DateTimeOffset CreatedAt);

public sealed record RunCodeResult(
    RunCodeOutcome Outcome,
    AcceptedCodeRun? Run = null);
