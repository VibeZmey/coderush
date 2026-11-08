using Domain.Models.Matches;
using Application.Common;

namespace Application.Features.Matches.TrainingSession;

public sealed record TrainingMatchView(Guid Id, DateTimeOffset CreatedAt, string Mode, string Status, string Difficulty,
    int Count, int TimePerProblemSeconds, DateTimeOffset ServerTime, DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt, DateTimeOffset? CompletedAt, string? FinishReason,
    TrainingProblemView? CurrentProblem, TrainingMatchSummary Result);

public sealed record TrainingProblemView(Guid Id, int Position, string Status, string? Outcome,
    string Title, string DescriptionMarkdown, string? InputDescriptionMarkdown,
    string? OutputDescriptionMarkdown, string? ConstraintsMarkdown, string? ExplanationMarkdown,
    double CpuTimeLimitSeconds, int MemoryLimitKilobytes, DateTimeOffset? StartedAt,
    DateTimeOffset? SubmissionDeadlineAt, IReadOnlyList<TrainingLanguageView> Languages,
    IReadOnlyList<TrainingSampleView> Samples);

public sealed record TrainingLanguageView(Guid Id, int ProgrammingLanguageId, string Name,
    string FileExtension, string Template, string? DraftSource, string? DraftInput);
public sealed record TrainingSampleView(string Input, string ExpectedOutput);
public sealed record TrainingProblemResult(Guid Id, int Position, string Title, string Status,
    string? Outcome, DateTimeOffset? StartedAt, DateTimeOffset? ClosedAt, double SpentSeconds, int RunCount);
public sealed record TrainingMatchSummary(int Solved, int TimedOut, int Skipped, int Abandoned,
    int Started, int NotStarted, double WorkSeconds, IReadOnlyList<TrainingProblemResult> Problems);

public static class TrainingMatchViews
{
    public static TrainingMatchView Create(TrainingMatch match, DateTimeOffset now)
    {
        var problems = match.Problems.OrderBy(problem => problem.Position).ToArray();
        var current = match.Status is MatchStatus.InProgress or MatchStatus.Finalizing
            ? problems.FirstOrDefault(problem => problem.Status == MatchProblemStatus.Active) ??
                problems.LastOrDefault(problem => problem.StartedAt.HasValue)
            : null;
        var results = problems.Select(problem => new TrainingProblemResult(problem.Id, problem.Position,
            problem.ProblemVersion.Title, problem.Status.ToString(), problem.Outcome?.ToString(),
            problem.StartedAt, problem.ClosedAt, problem.StartedAt is { } start
                ? Math.Max(0, ((problem.ClosedAt ?? now) - start).TotalSeconds) : 0, problem.CodeRuns.Count)).ToArray();
        var editor = current is null ? null : CreateProblem(current);
        return new(match.Id, match.CreatedAt, "Training", match.Status.ToString(), match.Difficulty.ToString(), match.Count,
            match.TimePerProblemSeconds, now, match.StartedAt, match.FinishedAt, match.CompletedAt,
            match.FinishReason?.ToString(), editor, new(
                problems.Count(problem => problem.Outcome == TrainingProblemOutcome.Solved),
                problems.Count(problem => problem.Outcome == TrainingProblemOutcome.TimedOut),
                problems.Count(problem => problem.Outcome == TrainingProblemOutcome.Skipped),
                problems.Count(problem => problem.Outcome == TrainingProblemOutcome.Abandoned),
                problems.Count(problem => problem.StartedAt.HasValue),
                match.Count - problems.Count(problem => problem.StartedAt.HasValue), results.Sum(result => result.SpentSeconds), results));
    }

    public static TrainingProblemView CreateProblem(MatchProblem current)
    {
        var version = current.ProblemVersion;
        return new(current.Id, current.Position, current.Status.ToString(), current.Outcome?.ToString(),
            version.Title, version.DescriptionMarkdown, version.InputDescriptionMarkdown,
            version.OutputDescriptionMarkdown, version.ConstraintsMarkdown, version.ExplanationMarkdown,
            version.CpuTimeLimitSeconds, version.MemoryLimitKilobytes, current.StartedAt, current.SubmissionDeadlineAt,
            version.Languages.Where(language => language.ProgrammingLanguage.IsEnabled).Select(language =>
            {
                var draft = current.Drafts.SingleOrDefault(item => item.ProgrammingLanguageId == language.ProgrammingLanguageId);
                return new TrainingLanguageView(language.Id, language.ProgrammingLanguageId,
                    language.ProgrammingLanguage.DisplayName, language.ProgrammingLanguage.FileExtension,
                    DefaultCodeTemplates.Resolve(language.ProgrammingLanguageId, language.Template), draft?.SourceCode, draft?.CustomInput);
            }).ToArray(),
            version.TestCases.Where(test => test.IsSample).OrderBy(test => test.Position)
                .Select(test => new TrainingSampleView(test.Input, test.ExpectedOutput)).ToArray());
    }
}
