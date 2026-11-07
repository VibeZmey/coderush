using System.Text.Json;
using Api.Contracts.Auth;
using Api.Contracts.Code;
using Api.Contracts.Matches;
using Api.Validation;
using Domain.Models.Problems;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Tests.Api;

public sealed class RequestValidationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(0, ProblemDifficulty.Easy, 300)]
    [InlineData(101, ProblemDifficulty.Easy, 300)]
    [InlineData(1, ProblemDifficulty.Unspecified, 300)]
    [InlineData(1, (ProblemDifficulty)999, 300)]
    [InlineData(1, ProblemDifficulty.Easy, 0)]
    [InlineData(1, ProblemDifficulty.Easy, 86401)]
    public async Task RejectsTrainingSettingsBeforeCallingEndpoint(int count, ProblemDifficulty difficulty, int seconds) =>
        await AssertRejected(new CreateTrainingMatchRequest
        {
            Count = count, Difficulty = difficulty, TimePerProblemSeconds = seconds
        });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RejectsBlankAuthFieldsAtApiBoundary(string? value)
    {
        await AssertRejected(new LoginRequest { Email = value!, Password = "password" });
        await AssertRejected(new LoginRequest { Email = "player@example.com", Password = value! });
        await AssertRejected(new RegisterRequest { Email = "player@example.com", Password = "password", UserName = value! });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("print(1)\0")]
    public async Task RejectsInvalidSourceAtApiBoundary(string? source) =>
        await AssertRejected(CodeRequest(source!));

    [Fact]
    public async Task RejectsOversizedUtf8TextAtApiBoundary()
    {
        await AssertRejected(CodeRequest(new string('я', 32769)));
        await AssertRejected(CodeRequest("print(1)", new string('я', 524289)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("hello\0")]
    public async Task RejectsNullOrUnstorableInputAtApiBoundary(string? input) =>
        await AssertRejected(CodeRequest("print(1)", input!));

    [Fact]
    public async Task ValidDefaultsAndEmptyStdinReachEndpoint()
    {
        await AssertAccepted(new CreateTrainingMatchRequest());
        await AssertAccepted(CodeRequest("print(1)"));
    }

    [Fact]
    public void JsonRequiresIdsAndSourceButDefaultsOmittedStdin()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StartMatchRequest>("{}", Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LoginRequest>("{}", Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RegisterRequest>("{}", Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RunCodeRequest>("{}", Options));
        var json = $$"""{"matchProblemId":"{{Guid.NewGuid()}}","programmingLanguageId":71,"sourceCode":"print(1)"}""";
        Assert.Empty(JsonSerializer.Deserialize<RunCodeRequest>(json, Options)!.StandardInput);
    }

    private static RunCodeRequest CodeRequest(string source, string input = "") => new()
    {
        MatchProblemId = Guid.NewGuid(), ProgrammingLanguageId = 71,
        SourceCode = source, StandardInput = input
    };

    private static async Task AssertRejected<T>(T request) where T : class
    {
        var invoked = false;
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), request);
        var result = await new RequestValidationFilter<T>().InvokeAsync(context, _ =>
        {
            invoked = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    private static async Task AssertAccepted<T>(T request) where T : class
    {
        var invoked = false;
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), request);
        await new RequestValidationFilter<T>().InvokeAsync(context, _ =>
        {
            invoked = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });
        Assert.True(invoked);
    }
}
