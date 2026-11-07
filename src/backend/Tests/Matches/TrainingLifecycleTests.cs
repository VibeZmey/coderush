using Application.Common.Interfaces;
using Application.Features.Matches.StartMatch;
using Application.Features.Matches.TrainingSession;
using Domain.Models.Execution;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Matches;

public sealed class TrainingLifecycleTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExpirationClosesOnlyCurrentTaskAndNextWaitsForPlayer()
    {
        var match = Session();
        Assert.Equal(Start.AddSeconds(60), match.Problems.First().SubmissionDeadlineAt);
        Assert.Null(match.Problems.Last().StartedAt);
        Assert.False(match.Refresh(Start.AddSeconds(59)));
        Assert.True(match.Refresh(Start.AddMinutes(10)));
        var first = match.Problems.First();
        Assert.Equal(TrainingProblemOutcome.TimedOut, first.Outcome);
        Assert.Equal(Start.AddSeconds(60), first.ClosedAt);
        Assert.Null(match.Problems.Last().StartedAt);
        Assert.Equal(MatchStatus.InProgress, match.Status);

        match.MoveNext(Start.AddHours(1));
        Assert.Equal(Start.AddHours(1), match.Problems.Last().StartedAt);
        Assert.Equal(Start.AddHours(1).AddSeconds(60), match.Problems.Last().SubmissionDeadlineAt);
        Assert.Equal(Start.AddSeconds(60), first.ClosedAt);
    }

    [Fact]
    public void SkippingAllTasksFinishesWithSavedOutcomes()
    {
        var match = Session();
        match.MoveNext(Start.AddSeconds(10));
        match.MoveNext(Start.AddSeconds(20));
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(MatchFinishReason.Finished, match.FinishReason);
        Assert.All(match.Problems, problem => Assert.Equal(TrainingProblemOutcome.Skipped, problem.Outcome));
        var view = TrainingMatchViews.Create(match, Start.AddSeconds(20));
        Assert.Equal(2, view.Result.Skipped);
        Assert.Equal(20, view.Result.WorkSeconds);
        Assert.Null(view.CurrentProblem);
    }

    [Fact]
    public void LeavingPreservesSolvedTaskAndDistinguishesUnopenedTasks()
    {
        var match = Session(3);
        var solved = match.Problems.First();
        solved.Close(TrainingProblemOutcome.Solved, TrainingProblemCloseReason.AcceptedSubmission,
            Start.AddSeconds(5), Start.AddSeconds(6));
        match.MoveNext(Start.AddSeconds(7));
        match.Leave(Start.AddSeconds(15));
        var view = TrainingMatchViews.Create(match, Start.AddSeconds(15));

        Assert.Equal(MatchFinishReason.Abandoned, match.FinishReason);
        Assert.Equal(1, view.Result.Solved);
        Assert.Equal(2, view.Result.Abandoned);
        Assert.Equal(2, view.Result.Started);
        Assert.Equal(1, view.Result.NotStarted);
        Assert.Null(match.Problems.Last().StartedAt);
        Assert.Equal(13, view.Result.WorkSeconds);
        match.Leave(Start.AddHours(2));
        Assert.Equal(Start.AddSeconds(15), match.FinishedAt);
    }

    [Fact]
    public void LateReadFinalizesLastTaskAtItsDeadlineInsteadOfAtReadTime()
    {
        var match = Session(1);
        match.Refresh(Start.AddHours(2));
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(Start.AddSeconds(60), match.FinishedAt);
        Assert.Equal(Start.AddHours(2), match.CompletedAt);
        Assert.Equal(MatchFinishReason.Finished, match.FinishReason);
    }

    [Fact]
    public void EditorContainsOnlySampleTestsAndAssignedTemplates()
    {
        var match = Session();
        var current = TrainingMatchViews.Create(match, Start).CurrentProblem!;
        Assert.Equal(1, current.Position);
        Assert.Equal("template-1", Assert.Single(current.Languages).Template);
        Assert.Equal("sample", Assert.Single(current.Samples).Input);
        Assert.DoesNotContain(current.Samples, sample => sample.Input == "hidden-secret");
    }

    [Fact]
    public async Task NotEnoughTasksDoesNotStartOrPartiallyAssignMatch()
    {
        var match = new TrainingMatch { UserId = "player", Count = 2 };
        var repository = new Repository(match) { Available = [Guid.NewGuid()] };
        var result = await new StartMatchHandler(repository).HandleAsync(new("player", match.Id));
        Assert.Equal(StartMatchOutcome.NotEnoughProblems, result.Outcome);
        Assert.Equal(MatchStatus.Created, match.Status);
        Assert.Null(match.StartedAt);
        Assert.Empty(match.Problems);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task ForeignSessionCannotBeReadOrAdvanced()
    {
        var match = Session();
        var repository = new Repository(match);
        Assert.Equal(TrainingSessionOutcome.NotFound,
            (await new GetTrainingMatchHandler(repository).HandleAsync(new("stranger", match.Id))).Outcome);
        Assert.Equal(TrainingSessionOutcome.NotFound,
            (await new NextTrainingProblemHandler(repository).HandleAsync(new("stranger", match.Id))).Outcome);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task ReturningToExpiredTaskDoesNotOpenNextTask()
    {
        var match = Session();
        var repository = new Repository(match);
        var result = await new GetTrainingMatchHandler(repository).HandleAsync(new("player", match.Id));
        Assert.Equal("TimedOut", result.Match!.CurrentProblem!.Outcome);
        Assert.Null(match.Problems.Last().StartedAt);
        Assert.Equal(1, repository.Saves);
    }

    [Fact]
    public async Task DraftIsUpdatedInPlaceAndRestoredInEditorView()
    {
        var match = Session(now: DateTimeOffset.UtcNow);
        var repository = new Repository(match);
        var problem = match.Problems.First();
        var handler = new SaveTrainingDraftHandler(repository);
        var result = await handler.HandleAsync(new("player", match.Id, problem.Id, 71, "  print(1)\n", "\n"));
        Assert.Equal(TrainingSessionOutcome.Success, result.Outcome);
        var draftId = problem.Drafts.Single().Id;
        await handler.HandleAsync(new("player", match.Id, problem.Id, 71, "print(2)", "test\n"));
        Assert.Equal(draftId, Assert.Single(problem.Drafts).Id);
        var language = TrainingMatchViews.Create(match, DateTimeOffset.UtcNow).CurrentProblem!.Languages.Single();
        Assert.Equal("print(2)", language.DraftSource);
        Assert.Equal("test\n", language.DraftInput);
    }

    private static TrainingMatch Session(int count = 2, DateTimeOffset? now = null)
    {
        var match = new TrainingMatch { UserId = "player", Count = count, TimePerProblemSeconds = 60 };
        for (var position = 1; position <= count; position++)
        {
            var version = new ProblemVersion { Title = $"Task {position}", DescriptionMarkdown = "Description", CreatedByUserId = "admin" };
            version.Languages.Add(new ProblemLanguage
            {
                ProblemVersionId = version.Id, ProgrammingLanguageId = 71, Template = $"template-{position}",
                ProgrammingLanguage = new ProgrammingLanguage { Id = 71, Key = "python", DisplayName = "Python", FileExtension = ".py", RuntimeVersion = "3" }
            });
            version.TestCases.Add(new ProblemTestCase { ProblemVersionId = version.Id, Input = "sample", ExpectedOutput = "visible", IsSample = true });
            version.TestCases.Add(new ProblemTestCase { ProblemVersionId = version.Id, Input = "hidden-secret", ExpectedOutput = "secret" });
            match.Problems.Add(new MatchProblem { MatchId = match.Id, Match = match, ProblemVersionId = version.Id, ProblemVersion = version, Position = position });
        }
        match.Start(now ?? Start);
        return match;
    }

    private sealed class Repository(Match match) : IMatchRepository
    {
        public int Saves { get; private set; }
        public IReadOnlyList<Guid> Available { get; init; } = [];
        public Task<Match?> GetByIdAsync(Guid id) => Task.FromResult<Match?>(id == match.Id ? match : null);
        public Task AddAsync(Match value) => throw new NotSupportedException();
        public Task UpdateAsync(Match value) { Saves++; return Task.CompletedTask; }
        public Task<IReadOnlyList<Guid>> SelectProblemVersionsAsync(ProblemDifficulty difficulty, int count) => Task.FromResult(Available);
        public Task<IReadOnlyList<Match>> ListByUserAsync(string userId) => Task.FromResult<IReadOnlyList<Match>>([match]);
    }
}
