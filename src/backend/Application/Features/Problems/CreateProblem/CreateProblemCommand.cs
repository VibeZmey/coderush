using Domain.Models.Problems;

namespace Application.Features.Problems.CreateProblem;

public sealed record CreateProblemCommand(
    string UserId,
    string Key,
    string Title,
    string DescriptionMarkdown,
    string? InputDescriptionMarkdown,
    string? OutputDescriptionMarkdown,
    string? ConstraintsMarkdown,
    string? ExplanationMarkdown,
    ProblemDifficulty Difficulty,
    bool Publish,
    double CpuTimeLimitSeconds,
    double WallTimeLimitSeconds,
    int MemoryLimitKilobytes,
    int OutputLimitKilobytes,
    IReadOnlyList<ProblemLanguageSettings> Languages,
    IReadOnlyList<ProblemTestCaseData> TestCases,
    bool IsPractice = false,
    string[]? Topics = null);

public sealed record ProblemLanguageSettings(
    int ProgrammingLanguageId,
    string Template);

public sealed record ProblemTestCaseData(string Input, string ExpectedOutput, bool IsSample);
