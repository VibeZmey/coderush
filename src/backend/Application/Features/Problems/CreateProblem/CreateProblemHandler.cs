using Application.Common.Interfaces;
using Domain.Models.Problems;

namespace Application.Features.Problems.CreateProblem;

public sealed class CreateProblemHandler(IProblemRepository problems)
    : IHandler<CreateProblemCommand, CreateProblemResult>
{
    public async Task<CreateProblemResult> HandleAsync(CreateProblemCommand request)
    {
        var languageIds = request.Languages.Select(language => language.ProgrammingLanguageId).ToArray();
        var enabledLanguages = await problems.GetEnabledLanguageIdsAsync(languageIds);
        // One setting per existing, enabled runtime. Repeated IDs cannot produce a full match.
        if (enabledLanguages.Count != languageIds.Length)
            return new(CreateProblemOutcome.LanguageUnavailable);

        var problem = new Problem { Key = request.Key, CreatedByUserId = request.UserId };
        var version = new ProblemVersion
        {
            ProblemId = problem.Id,
            CreatedByUserId = request.UserId,
            VersionNumber = 1,
            Title = request.Title,
            DescriptionMarkdown = request.DescriptionMarkdown,
            InputDescriptionMarkdown = request.InputDescriptionMarkdown,
            OutputDescriptionMarkdown = request.OutputDescriptionMarkdown,
            ConstraintsMarkdown = request.ConstraintsMarkdown,
            ExplanationMarkdown = request.ExplanationMarkdown,
            Difficulty = request.Difficulty,
            CpuTimeLimitSeconds = request.CpuTimeLimitSeconds,
            WallTimeLimitSeconds = request.WallTimeLimitSeconds,
            MemoryLimitKilobytes = request.MemoryLimitKilobytes,
            OutputLimitKilobytes = request.OutputLimitKilobytes,
            Status = request.Publish ? PublicationStatus.Published : PublicationStatus.Draft,
            PublishedAt = request.Publish ? DateTimeOffset.UtcNow : null
        };
        foreach (var language in request.Languages)
            version.Languages.Add(new ProblemLanguage
            {
                ProblemVersionId = version.Id,
                ProgrammingLanguageId = language.ProgrammingLanguageId,
                Template = language.Template
            });

        for (var index = 0; index < request.TestCases.Count; index++)
        {
            var test = request.TestCases[index];
            version.TestCases.Add(new ProblemTestCase
            {
                ProblemVersionId = version.Id,
                Position = index + 1,
                Input = test.Input,
                ExpectedOutput = test.ExpectedOutput,
                IsSample = test.IsSample
            });
        }
        problem.Versions.Add(version);

        if (!await problems.TryAddAsync(problem))
            return new(CreateProblemOutcome.DuplicateKey);

        return new(CreateProblemOutcome.Created,
            new CreatedProblem(problem.Id, version.Id, version.VersionNumber, version.Status, problem.CreatedAt));
    }
}
