using Domain.Models.Problems;

namespace Application.Features.Problems.CreateProblem;

public enum CreateProblemOutcome
{
    Created,
    DuplicateKey,
    LanguageUnavailable
}

public sealed record CreatedProblem(
    Guid Id, Guid VersionId, int VersionNumber, PublicationStatus Status, DateTimeOffset CreatedAt);

public sealed record CreateProblemResult(CreateProblemOutcome Outcome, CreatedProblem? Problem = null);
