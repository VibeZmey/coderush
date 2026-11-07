using Domain.Models.Matches;

namespace Application.Features.Matches.StartMatch;

public enum StartMatchOutcome
{
    Started,
    NotFound,
    InvalidState,
    NotEnoughProblems
}

public sealed record StartedMatch(Guid Id, MatchStatus Status, DateTimeOffset StartedAt);

public sealed record StartMatchResult(StartMatchOutcome Outcome, StartedMatch? Match = null);
