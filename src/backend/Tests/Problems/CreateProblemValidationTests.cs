using System.Text.Json;
using Api.Contracts.Problems;
using Api.Validation;
using Domain.Models.Problems;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Tests.Problems;

public sealed class CreateProblemValidationTests
{
    [Fact]
    public async Task ValidProblemAllowsEmptyTemplateAndEmptyTestInputOrOutput() =>
        Assert.True(await ReachesEndpoint(Request()));

    [Fact]
    public async Task EmptyLanguageListIsRejected()
    {
        var request = Request();
        request.Languages.Clear();
        Assert.False(await ReachesEndpoint(request));
    }

    [Fact]
    public async Task EmptyTestListIsRejected()
    {
        var request = Request();
        request.TestCases.Clear();
        Assert.False(await ReachesEndpoint(request));
    }

    [Fact]
    public async Task InvalidProblemLimitsAreRejected()
    {
        var request = new CreateProblemRequest
        {
            Key = "test-problem", Title = "Test", DescriptionMarkdown = "Description",
            Difficulty = ProblemDifficulty.Easy, MemoryLimitKilobytes = 0,
            Languages = [new() { ProgrammingLanguageId = 71, Template = "" }],
            TestCases = [new() { Input = "", ExpectedOutput = "" }]
        };
        Assert.False(await ReachesEndpoint(request));
    }

    [Fact]
    public async Task InvalidNestedTestTextIsRejected()
    {
        var request = Request();
        request.TestCases[0] = new CreateProblemTestCaseRequest { Input = "\0", ExpectedOutput = "" };
        Assert.False(await ReachesEndpoint(request));
    }

    [Fact]
    public async Task NullCollectionItemReturnsValidationFailure()
    {
        var request = Request();
        request.Languages[0] = null!;
        Assert.False(await ReachesEndpoint(request));
    }

    [Fact]
    public void RequiredFieldsAndClientSuppliedAuthorAreRejectedByJsonContract()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateProblemRequest>("{}", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateProblemRequest>("""{"createdByUserId":"somebody"}""", options));
    }

    [Fact]
    public void LimitsAreAtProblemLevelAndLanguageLimitsAreRejected()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Request(), options));
        Assert.Equal(128000, json.RootElement.GetProperty("memoryLimitKilobytes").GetInt32());
        Assert.False(json.RootElement.GetProperty("languages")[0].TryGetProperty("memoryLimitKilobytes", out _));
        var oldLanguagePayload = $$"""{"programmingLanguageId":71,"template":"","memoryLimitKilobytes":256000}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateProblemLanguageRequest>(oldLanguagePayload, options));
    }

    private static CreateProblemRequest Request() => new()
    {
        Key = "test-problem", Title = "Test", DescriptionMarkdown = "Description",
        Difficulty = ProblemDifficulty.Easy,
        Languages = [new() { ProgrammingLanguageId = 71, Template = "" }],
        TestCases = [new() { Input = "", ExpectedOutput = "" }]
    };

    private static async Task<bool> ReachesEndpoint(CreateProblemRequest request)
    {
        var invoked = false;
        var result = await new RequestValidationFilter<CreateProblemRequest>().InvokeAsync(
            new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), request), _ =>
            {
                invoked = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });
        if (!invoked)
            Assert.Equal(400, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        return invoked;
    }
}
