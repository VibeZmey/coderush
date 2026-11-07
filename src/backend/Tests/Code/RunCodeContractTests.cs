using System.Text.Json;
using Api.Contracts.Code;
using Domain.Models.Execution;
using Xunit;

namespace Tests.Code;

public sealed class RunCodeContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ResponseContainsRunIdAndPendingStatus()
    {
        var response = new RunCodeResponse(Guid.NewGuid(), ProcessingStatus.Pending, DateTimeOffset.UtcNow);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, Options));

        Assert.Equal(response.Id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Pending", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(3, json.RootElement.EnumerateObject().Count());
    }

    [Theory]
    [InlineData("""{"userId":"another-user"}""")]
    [InlineData("""{"cpuTimeLimitSeconds":1000}""")]
    [InlineData("""{"expectedOutput":"hidden"}""")]
    public void RejectsClientOwnedIdentityLimitsAndExpectedAnswers(string payload) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RunCodeRequest>(payload, Options));
}
