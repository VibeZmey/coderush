using Application.Common.Interfaces;
using Application.Features.Problems.CreateProblem;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Problems;

public sealed class CreateProblemTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavesWholeProblemWithTemplatesAndOrderedTests(bool publish)
    {
        var languageId = 71;
        var repository = new RecordingRepository([languageId]);
        var request = Command(languageId) with { Publish = publish };

        var result = await new CreateProblemHandler(repository).HandleAsync(request);

        Assert.Equal(CreateProblemOutcome.Created, result.Outcome);
        var problem = Assert.Single(repository.Problems);
        Assert.Equal(request.Key, problem.Key);
        Assert.Equal(request.UserId, problem.CreatedByUserId);
        var version = Assert.Single(problem.Versions);
        Assert.Equal(problem.Id, version.ProblemId);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(request.Title, version.Title);
        Assert.Equal(request.DescriptionMarkdown, version.DescriptionMarkdown);
        Assert.Equal(request.ConstraintsMarkdown, version.ConstraintsMarkdown);
        Assert.Equal(request.UserId, version.CreatedByUserId);
        Assert.Equal(request.Difficulty, version.Difficulty);
        Assert.Equal(publish ? PublicationStatus.Published : PublicationStatus.Draft, version.Status);
        Assert.Equal(publish, version.PublishedAt.HasValue);
        if (publish)
            Assert.Equal(TimeSpan.Zero, version.PublishedAt!.Value.Offset);
        var language = Assert.Single(version.Languages);
        Assert.Equal(languageId, language.ProgrammingLanguageId);
        Assert.Equal(version.Id, language.ProblemVersionId);
        Assert.Equal(request.Languages[0].Template, language.Template);
        Assert.Equal(request.CpuTimeLimitSeconds, version.CpuTimeLimitSeconds);
        Assert.Equal(request.WallTimeLimitSeconds, version.WallTimeLimitSeconds);
        Assert.Equal(256000, version.MemoryLimitKilobytes);
        Assert.Equal(request.OutputLimitKilobytes, version.OutputLimitKilobytes);
        var tests = version.TestCases.ToArray();
        Assert.Equal(2, tests.Length);
        Assert.Equal(new[] { 1, 2 }, tests.Select(test => test.Position));
        Assert.Equal("2 3\n", tests[0].Input);
        Assert.Equal("5\n", tests[0].ExpectedOutput);
        Assert.True(tests[0].IsSample);
        Assert.False(tests[1].IsSample);
        Assert.Equal(problem.Id, result.Problem!.Id);
        Assert.Equal(version.Id, result.Problem.VersionId);
    }

    [Fact]
    public async Task UnavailableLanguagePreventsAnySave()
    {
        var repository = new RecordingRepository([]);
        var result = await new CreateProblemHandler(repository).HandleAsync(Command(999));

        Assert.Equal(CreateProblemOutcome.LanguageUnavailable, result.Outcome);
        Assert.Empty(repository.Problems);
    }

    [Fact]
    public async Task RepeatedLanguagePreventsAnySave()
    {
        var languageId = 71;
        var repository = new RecordingRepository([languageId]);
        var request = Command(languageId);
        request = request with { Languages = [request.Languages[0], request.Languages[0]] };
        var result = await new CreateProblemHandler(repository).HandleAsync(request);

        Assert.Equal(CreateProblemOutcome.LanguageUnavailable, result.Outcome);
        Assert.Empty(repository.Problems);
    }

    [Fact]
    public async Task DuplicateKeyReturnsConflictOutcome()
    {
        var languageId = 71;
        var repository = new RecordingRepository([languageId]) { CanSave = false };
        var result = await new CreateProblemHandler(repository).HandleAsync(Command(languageId));

        Assert.Equal(CreateProblemOutcome.DuplicateKey, result.Outcome);
        Assert.Null(result.Problem);
    }

    private static CreateProblemCommand Command(int languageId) => new(
        "admin-id", "sum-two-numbers", "Сумма чисел", "**Сложите** два числа.",
        "Два числа", "Сумма", "Целые числа", "Пример", ProblemDifficulty.Easy, false,
        2, 10, 256000, 1024,
        [new(languageId, "a, b = map(int, input().split())\n")],
        [new("2 3\n", "5\n", true), new("-10 4\n", "-6\n", false)]);

    [Fact]
    public async Task SameJudge0LanguageHasIndependentTemplatesForDifferentProblems()
    {
        var repository = new RecordingRepository([71]);
        var handler = new CreateProblemHandler(repository);
        await handler.HandleAsync(Command(71));
        await handler.HandleAsync(Command(71) with
        {
            Key = "another-problem", Languages = [new(71, "print('another template')")]
        });

        var first = repository.Problems[0].Versions.Single().Languages.Single();
        var second = repository.Problems[1].Versions.Single().Languages.Single();
        Assert.Equal(first.ProgrammingLanguageId, second.ProgrammingLanguageId);
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Template, second.Template);
    }

    private sealed class RecordingRepository(int[] enabledLanguageIds) : IProblemRepository
    {
        public List<Problem> Problems { get; } = [];
        public bool CanSave { get; init; } = true;

        public Task<IReadOnlyList<int>> GetEnabledLanguageIdsAsync(IReadOnlyCollection<int> ids) =>
            Task.FromResult<IReadOnlyList<int>>(enabledLanguageIds.Where(ids.Contains).Distinct().ToArray());

        public Task<bool> TryAddAsync(Problem problem)
        {
            if (CanSave)
                Problems.Add(problem);
            return Task.FromResult(CanSave);
        }
    }
}
