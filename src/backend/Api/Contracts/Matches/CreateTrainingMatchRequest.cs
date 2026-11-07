using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Domain.Models.Problems;

namespace Api.Contracts.Matches;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateTrainingMatchRequest
{
    [Range(1, 100)]
    public int Count { get; init; } = 2;

    [JsonConverter(typeof(JsonStringEnumConverter<ProblemDifficulty>))]
    [Range(1, 3)]
    public ProblemDifficulty Difficulty { get; init; } = ProblemDifficulty.Easy;

    [Range(1, 86400)]
    public int TimePerProblemSeconds { get; init; } = 300;
}
