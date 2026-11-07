using Application.Common.Interfaces;
using Application.Features.Matches.StartMatch;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Matches;

public sealed class StartMatchTests
{
    [Fact]
    public async Task StartsOwnedCreatedMatchAndSavesUtcTimestamp()
    {
        var match = new TrainingMatch { UserId = "player" };
        var repository = new RecordingMatchRepository(match);
        var before = DateTimeOffset.UtcNow;

        var result = await new StartMatchHandler(repository)
            .HandleAsync(new StartMatchCommand("player", match.Id));

        Assert.Equal(StartMatchOutcome.Started, result.Outcome);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Equal(1, repository.UpdateCount);
        Assert.Equal(match.Id, result.Match!.Id);
        Assert.Equal(match.StartedAt, result.Match.StartedAt);
        Assert.Equal(TimeSpan.Zero, result.Match.StartedAt.Offset);
        Assert.InRange(result.Match.StartedAt, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task MissingMatchReturnsNotFoundWithoutSaving()
    {
        var repository = new RecordingMatchRepository(null);
        var result = await new StartMatchHandler(repository)
            .HandleAsync(new StartMatchCommand("player", Guid.NewGuid()));

        Assert.Equal(StartMatchOutcome.NotFound, result.Outcome);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public async Task AnotherPlayersMatchIsNotDisclosedOrChanged()
    {
        var match = new TrainingMatch { UserId = "owner" };
        var repository = new RecordingMatchRepository(match);
        var result = await new StartMatchHandler(repository)
            .HandleAsync(new StartMatchCommand("another-player", match.Id));

        Assert.Equal(StartMatchOutcome.NotFound, result.Outcome);
        Assert.Equal(MatchStatus.Created, match.Status);
        Assert.Null(match.StartedAt);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Theory]
    [InlineData(MatchStatus.InProgress)]
    [InlineData(MatchStatus.Finalizing)]
    [InlineData(MatchStatus.Completed)]
    [InlineData(MatchStatus.Cancelled)]
    public async Task NonCreatedMatchReturnsConflictWithoutChangingStartTime(MatchStatus status)
    {
        var originalStart = DateTimeOffset.UtcNow.AddMinutes(-5);
        var match = new TrainingMatch { UserId = "player", Status = status, StartedAt = originalStart };
        var repository = new RecordingMatchRepository(match);
        var result = await new StartMatchHandler(repository)
            .HandleAsync(new StartMatchCommand("player", match.Id));

        Assert.Equal(StartMatchOutcome.InvalidState, result.Outcome);
        Assert.Equal(status, match.Status);
        Assert.Equal(originalStart, match.StartedAt);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void DomainStartRejectsRepeatedStartAndPreservesTimestamp()
    {
        var match = new TrainingMatch { UserId = "player", Count = 1 };
        match.Problems.Add(new MatchProblem { MatchId = match.Id, ProblemVersionId = Guid.NewGuid(), Position = 1 });
        match.Start();
        var firstStartedAt = match.StartedAt;

        Assert.Throws<InvalidOperationException>(() => match.Start());
        Assert.Equal(firstStartedAt, match.StartedAt);
    }

    private sealed class RecordingMatchRepository(Match? match) : IMatchRepository
    {
        public int ReadCount { get; private set; }
        public int UpdateCount { get; private set; }

        public Task<Match?> GetByIdAsync(Guid id)
        {
            ReadCount++;
            return Task.FromResult(match?.Id == id ? match : null);
        }

        public Task AddAsync(Match value) => throw new NotSupportedException();

        public Task UpdateAsync(Match value)
        {
            Assert.Same(match, value);
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> SelectProblemVersionsAsync(ProblemDifficulty difficulty, int count) =>
            Task.FromResult<IReadOnlyList<Guid>>(Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray());
        public Task<IReadOnlyList<Match>> ListByUserAsync(string userId) =>
            Task.FromResult<IReadOnlyList<Match>>(match is null ? [] : [match]);
    }
}
