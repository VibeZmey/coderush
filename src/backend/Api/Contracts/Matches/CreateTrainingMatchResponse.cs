using System.Text.Json.Serialization;
using Domain.Models.Matches;

namespace Api.Contracts.Matches;

public sealed record CreateTrainingMatchResponse(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))] MatchStatus Status,
    DateTimeOffset CreatedAt);
