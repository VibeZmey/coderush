using System.Text.Json.Serialization;
using Domain.Models.Matches;

namespace Api.Contracts.Matches;

public sealed record StartMatchResponse(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))] MatchStatus Status,
    DateTimeOffset StartedAt);
