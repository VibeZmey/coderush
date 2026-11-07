using Application.Common.Interfaces;
using Application.Features.Code.RunCode;
using Domain.Models.Execution;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Code;

public sealed class RunCodeTests
{
    [Fact]
    public async Task DuelRunBelongsToTheParticipantWhoRequestedIt()
    {
        var problem = ActiveProblem();
        problem.Match = new DuelMatch { UserId = "host", OpponentId = "player", Status = MatchStatus.InProgress };
        var repository = new RecordingRepository(problem);
        var handler = new RunCodeHandler(repository);
        var command = Command(problem);

        Assert.Equal(RunCodeOutcome.NotFound, (await handler.HandleAsync(command with { UserId = "outsider" })).Outcome);
        Assert.Equal(RunCodeOutcome.Accepted, (await handler.HandleAsync(command)).Outcome);
        Assert.Equal("player", Assert.Single(repository.Runs).DuelPlayerId);
    }

    [Fact]
    public async Task SavesPendingRunAndJobWithUnchangedCodeInputAndServerLimits()
    {
        var problem = ActiveProblem();
        var repository = new RecordingRepository(problem);
        const string source = "  print(input())\n";
        const string input = "  привет\n\n";
        var result = await new RunCodeHandler(repository).HandleAsync(Command(problem, source, input));

        Assert.Equal(RunCodeOutcome.Accepted, result.Outcome);
        var run = Assert.Single(repository.Runs);
        Assert.Equal(problem.Id, run.MatchProblemId);
        Assert.Equal(run.Id, result.Run!.Id);
        Assert.Equal(ProcessingStatus.Pending, result.Run.Status);
        var job = run.ExecutionJob;
        Assert.Equal(job.Id, run.ExecutionJobId);
        Assert.Equal(source, job.SourceCode);
        Assert.Equal(input, job.StandardInput);
        Assert.Equal(problem.ProblemVersion.Languages.Single().ProgrammingLanguageId, job.ProgrammingLanguageId);
        Assert.Equal(3.5, job.CpuTimeLimitSeconds);
        Assert.Equal(15, job.WallTimeLimitSeconds);
        Assert.Equal(256000, job.MemoryLimitKilobytes);
        Assert.Equal(2048, job.OutputLimitKilobytes);
        Assert.Equal(ProcessingStatus.Pending, job.Status);
        Assert.Null(job.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrForeignTaskDoesNotCreateRun(bool missing)
    {
        var problem = ActiveProblem();
        var command = Command(problem);
        problem.Match.UserId = "another-player";
        var repository = new RecordingRepository(missing ? null : problem);
        var result = await new RunCodeHandler(repository).HandleAsync(command);

        Assert.Equal(RunCodeOutcome.NotFound, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    [Theory]
    [InlineData(MatchStatus.Created)]
    [InlineData(MatchStatus.Finalizing)]
    [InlineData(MatchStatus.Completed)]
    [InlineData(MatchStatus.Cancelled)]
    public async Task NonRunningMatchRejectsRun(MatchStatus status)
    {
        var problem = ActiveProblem();
        problem.Match.Status = status;
        var repository = new RecordingRepository(problem);
        var result = await new RunCodeHandler(repository).HandleAsync(Command(problem));

        Assert.Equal(RunCodeOutcome.InvalidState, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    [Theory]
    [InlineData(MatchProblemStatus.NotStarted)]
    [InlineData(MatchProblemStatus.Finalizing)]
    [InlineData(MatchProblemStatus.Completed)]
    public async Task NonActiveTaskRejectsRun(MatchProblemStatus status)
    {
        var problem = ActiveProblem();
        problem.Status = status;
        var repository = new RecordingRepository(problem);
        var result = await new RunCodeHandler(repository).HandleAsync(Command(problem));

        Assert.Equal(RunCodeOutcome.InvalidState, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    [Fact]
    public async Task ExpiredTaskRejectsRun()
    {
        var problem = ActiveProblem();
        problem.SubmissionDeadlineAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        var repository = new RecordingRepository(problem);
        var result = await new RunCodeHandler(repository).HandleAsync(Command(problem));

        Assert.Equal(RunCodeOutcome.InvalidState, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    [Fact]
    public async Task ClosedTaskRejectsRunEvenIfDeadlineIsInFuture()
    {
        var problem = ActiveProblem();
        problem.ClosedAt = DateTimeOffset.UtcNow;
        var repository = new RecordingRepository(problem);
        var result = await new RunCodeHandler(repository).HandleAsync(Command(problem));

        Assert.Equal(RunCodeOutcome.InvalidState, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisabledOrUnlistedLanguageRejectsRun(bool disabled)
    {
        var problem = ActiveProblem();
        var command = Command(problem);
        if (disabled)
            problem.ProblemVersion.Languages.Single().ProgrammingLanguage.IsEnabled = false;
        else
            problem.ProblemVersion.Languages.Clear();
        var repository = new RecordingRepository(problem);
        var result = await new RunCodeHandler(repository).HandleAsync(command);

        Assert.Equal(RunCodeOutcome.LanguageUnavailable, result.Outcome);
        Assert.Empty(repository.Runs);
    }

    private static RunCodeCommand Command(MatchProblem problem, string source = "print(1)", string input = "") =>
        new("player", problem.Id, problem.ProblemVersion.Languages.Single().ProgrammingLanguageId, source, input);

    [Fact]
    public async Task DifferentLanguagesUseTheSameProblemVersionLimits()
    {
        var problem = ActiveProblem();
        var otherLanguage = new ProgrammingLanguage
        {
            Id = 63,
            Key = "another-runtime", DisplayName = "Another runtime", FileExtension = ".txt", RuntimeVersion = "test"
        };
        problem.ProblemVersion.Languages.Add(new ProblemLanguage
        {
            ProblemVersionId = problem.ProblemVersionId,
            ProgrammingLanguageId = otherLanguage.Id, ProgrammingLanguage = otherLanguage, Template = ""
        });
        var repository = new RecordingRepository(problem);
        var handler = new RunCodeHandler(repository);
        foreach (var language in problem.ProblemVersion.Languages)
        {
            var result = await handler.HandleAsync(new RunCodeCommand(
                "player", problem.Id, language.ProgrammingLanguageId, "source", ""));
            Assert.Equal(RunCodeOutcome.Accepted, result.Outcome);
        }

        Assert.Equal(2, repository.Runs.Count);
        Assert.All(repository.Runs, run =>
        {
            Assert.Equal(3.5, run.ExecutionJob.CpuTimeLimitSeconds);
            Assert.Equal(15, run.ExecutionJob.WallTimeLimitSeconds);
            Assert.Equal(256000, run.ExecutionJob.MemoryLimitKilobytes);
            Assert.Equal(2048, run.ExecutionJob.OutputLimitKilobytes);
        });
    }

    private static MatchProblem ActiveProblem()
    {
        var language = new ProgrammingLanguage
        {
            Id = 71,
            Key = "python-test", DisplayName = "Python", FileExtension = ".py", RuntimeVersion = "test"
        };
        var version = new ProblemVersion
        {
            Title = "Task", DescriptionMarkdown = "Description", CreatedByUserId = "admin",
            CpuTimeLimitSeconds = 3.5, WallTimeLimitSeconds = 15,
            MemoryLimitKilobytes = 256000, OutputLimitKilobytes = 2048
        };
        version.Languages.Add(new ProblemLanguage
        {
            ProgrammingLanguageId = language.Id, ProgrammingLanguage = language,
            ProblemVersionId = version.Id, Template = ""
        });
        var match = new TrainingMatch { UserId = "player", Status = MatchStatus.InProgress };
        return new MatchProblem
        {
            MatchId = match.Id, Match = match, ProblemVersionId = version.Id, ProblemVersion = version,
            Status = MatchProblemStatus.Active, StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            SubmissionDeadlineAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };
    }

    private sealed class RecordingRepository(MatchProblem? problem) : ICodeRunRepository
    {
        public List<CodeRun> Runs { get; } = [];
        public int Reads { get; private set; }

        public Task<MatchProblem?> GetMatchProblemAsync(Guid id)
        {
            Reads++;
            return Task.FromResult(problem?.Id == id ? problem : null);
        }

        public Task AddAsync(CodeRun run)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }
    }
}
