using System.Text.Json.Serialization;
using Domain.Models.Problems;
using Application.Features.Problems.CreateProblem;

namespace Application.Features.Problems.ManageProblems;

public sealed record ProblemSearch(string Search, ProblemDifficulty? Difficulty, PublicationStatus? Status,
    string Sort, int Page, int PageSize, bool? IsPractice = null);

public sealed record ProblemSummary(Guid Id, string Key, Guid VersionId, int VersionNumber, string Title,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProblemDifficulty>))] ProblemDifficulty Difficulty,
    [property: JsonConverter(typeof(JsonStringEnumConverter<PublicationStatus>))] PublicationStatus Status,
    int TestCount, int LanguageCount, DateTimeOffset CreatedAt, bool IsPractice, string[] Topics);

public sealed record ProblemPage(IReadOnlyList<ProblemSummary> Items, int Total, int Page, int PageSize);
public sealed record ProblemDetails(ProblemSummary Summary, string DescriptionMarkdown,
    string? InputDescriptionMarkdown, string? OutputDescriptionMarkdown, string? ConstraintsMarkdown,
    string? ExplanationMarkdown, double CpuTimeLimitSeconds, double WallTimeLimitSeconds,
    int MemoryLimitKilobytes, int OutputLimitKilobytes,
    IReadOnlyList<ProblemLanguageSettings> Languages, IReadOnlyList<ProblemTestCaseData> TestCases);

public enum ProblemWriteOutcome { Saved, NotFound, Conflict, PoolChange, DuplicateKey, LanguageUnavailable }
public sealed record ProblemWriteResult(ProblemWriteOutcome Outcome, CreatedProblem? Problem = null);
public sealed record ProblemImportResult(ProblemWriteOutcome Outcome, IReadOnlyList<CreatedProblem>? Problems = null);

public interface IProblemManagementRepository
{
    Task<ProblemPage> SearchAsync(ProblemSearch search, CancellationToken cancellationToken);
    Task<ProblemDetails?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Problem?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<ProblemWriteOutcome> SaveVersionAsync(Problem problem, ProblemVersion version, CancellationToken cancellationToken);
    Task<ProblemWriteOutcome> ImportAsync(IReadOnlyList<Problem> problems, CancellationToken cancellationToken);
}

public sealed class ProblemManagementHandler(
    IProblemManagementRepository repository, Application.Common.Interfaces.IProblemRepository languages)
{
    public async Task<ProblemWriteResult> UpdateAsync(Guid id, Guid baseVersionId, CreateProblemCommand command, CancellationToken cancellationToken)
    {
        var problem = await repository.GetForUpdateAsync(id, cancellationToken);
        if (problem is null) return new(ProblemWriteOutcome.NotFound);
        if (problem.IsPractice != command.IsPractice) return new(ProblemWriteOutcome.PoolChange);
        var current = problem.Versions.MaxBy(v => v.VersionNumber)!;
        if (current.Id != baseVersionId) return new(ProblemWriteOutcome.Conflict);
        if (!await LanguagesAvailable(command)) return new(ProblemWriteOutcome.LanguageUnavailable);
        // Every edit creates a new version. Existing matches retain their exact tests.
        var version = BuildVersion(problem.Id, current.VersionNumber + 1, command);
        problem.Key = command.Key;
        var result = await repository.SaveVersionAsync(problem, version, cancellationToken);
        return new(result, result == ProblemWriteOutcome.Saved
            ? new CreatedProblem(problem.Id, version.Id, version.VersionNumber, version.Status, problem.CreatedAt) : null);
    }

    public async Task<ProblemImportResult> ImportAsync(IReadOnlyList<CreateProblemCommand> commands, CancellationToken cancellationToken)
    {
        if (commands.Select(c => c.Key).Distinct().Count() != commands.Count)
            return new(ProblemWriteOutcome.DuplicateKey);
        foreach (var command in commands)
            if (!await LanguagesAvailable(command)) return new(ProblemWriteOutcome.LanguageUnavailable);
        var problems = commands.Select(command =>
        {
            var problem = new Problem { Key = command.Key, CreatedByUserId = command.UserId, IsPractice = command.IsPractice };
            problem.Versions.Add(BuildVersion(problem.Id, 1, command));
            return problem;
        }).ToArray();
        var result = await repository.ImportAsync(problems, cancellationToken);
        return new(result, result == ProblemWriteOutcome.Saved ? problems.Select(p =>
        {
            var v = p.Versions.Single();
            return new CreatedProblem(p.Id, v.Id, v.VersionNumber, v.Status, p.CreatedAt);
        }).ToArray() : null);
    }

    private async Task<bool> LanguagesAvailable(CreateProblemCommand command)
    {
        var ids = command.Languages.Select(l => l.ProgrammingLanguageId).ToArray();
        return (await languages.GetEnabledLanguageIdsAsync(ids)).Count == ids.Length;
    }

    private static ProblemVersion BuildVersion(Guid problemId, int number, CreateProblemCommand command)
    {
        var version = new ProblemVersion
        {
            ProblemId = problemId, VersionNumber = number, CreatedByUserId = command.UserId,
            Title = command.Title, DescriptionMarkdown = command.DescriptionMarkdown,
            InputDescriptionMarkdown = command.InputDescriptionMarkdown, OutputDescriptionMarkdown = command.OutputDescriptionMarkdown,
            ConstraintsMarkdown = command.ConstraintsMarkdown, ExplanationMarkdown = command.ExplanationMarkdown,
            Topics = command.Topics ?? [],
            Difficulty = command.Difficulty, Status = command.Publish ? PublicationStatus.Published : PublicationStatus.Draft,
            PublishedAt = command.Publish ? DateTimeOffset.UtcNow : null,
            CpuTimeLimitSeconds = command.CpuTimeLimitSeconds, WallTimeLimitSeconds = command.WallTimeLimitSeconds,
            MemoryLimitKilobytes = command.MemoryLimitKilobytes, OutputLimitKilobytes = command.OutputLimitKilobytes
        };
        foreach (var language in command.Languages)
            version.Languages.Add(new ProblemLanguage { ProblemVersionId = version.Id, ProgrammingLanguageId = language.ProgrammingLanguageId, Template = language.Template });
        for (var i = 0; i < command.TestCases.Count; i++)
        {
            var test = command.TestCases[i];
            version.TestCases.Add(new ProblemTestCase { ProblemVersionId = version.Id, Position = i + 1, Input = test.Input, ExpectedOutput = test.ExpectedOutput, IsSample = test.IsSample });
        }
        return version;
    }
}
