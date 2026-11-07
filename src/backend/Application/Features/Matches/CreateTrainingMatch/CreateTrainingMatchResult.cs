using Domain.Models.Matches;

namespace Application.Features.Matches.CreateTrainingMatch;

public sealed record CreateTrainingMatchResult(Guid Id, MatchStatus Status, DateTimeOffset CreatedAt);
