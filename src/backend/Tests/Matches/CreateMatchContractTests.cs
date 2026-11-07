using System.Text.Json;
using Api.Contracts.Matches;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Matches;

public sealed class CreateMatchContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ReadsTrainingSettingsWithoutTypeDiscriminator()
    {
        var request = JsonSerializer.Deserialize<CreateTrainingMatchRequest>(
            """{"count":10,"difficulty":"Hard","timePerProblemSeconds":300}""", Options);

        var training = Assert.IsType<CreateTrainingMatchRequest>(request);
        Assert.Equal(10, training.Count);
        Assert.Equal(ProblemDifficulty.Hard, training.Difficulty);
    }

    [Fact]
    public void OmittedSettingsUseTrainingDefaults()
    {
        var request = JsonSerializer.Deserialize<CreateTrainingMatchRequest>("{}", Options);

        Assert.NotNull(request);
        Assert.Equal(2, request.Count);
        Assert.Equal(ProblemDifficulty.Easy, request.Difficulty);
        Assert.Equal(300, request.TimePerProblemSeconds);
    }

    [Theory]
    [InlineData("""{"type":"training"}""")]
    [InlineData("""{"userId":"another-player"}""")]
    [InlineData("""{"difficulty":"Impossible"}""")]
    [InlineData("""{"count":"ten"}""")]
    public void RejectsUnsupportedOrMalformedRequests(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateTrainingMatchRequest>(json, Options));
    }
}
