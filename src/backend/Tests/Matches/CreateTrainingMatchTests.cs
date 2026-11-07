using Application.Common.Interfaces;
using Application.Features.Matches.CreateTrainingMatch;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Xunit;

namespace Tests.Matches;

public sealed class CreateTrainingMatchTests
{
    [Fact]
    public async Task CreatesOwnedSessionWithRequestedSettingsWithoutStartingIt()
    {
        var repository = new RecordingMatchRepository();
        var handler = new CreateTrainingMatchHandler(repository);
        var result = await handler.HandleAsync(new("player-id", 10, ProblemDifficulty.Hard, 300));

        var match = Assert.IsType<TrainingMatch>(Assert.Single(repository.Matches));
        Assert.Equal("player-id", match.UserId);
        Assert.Equal(10, match.Count);
        Assert.Equal(ProblemDifficulty.Hard, match.Difficulty);
        Assert.Equal(300, match.TimePerProblemSeconds);
        Assert.Equal(MatchStatus.Created, match.Status);
        Assert.Null(match.StartedAt);
        Assert.Null(match.CompletedAt);
        Assert.Empty(match.Problems);
        Assert.Equal(match.Id, result.Id);
    }

    private sealed class RecordingMatchRepository : IMatchRepository
    {
        public List<Match> Matches { get; } = [];

        public Task<Match?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Match match)
        {
            Matches.Add(match);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Match match)
        {
            throw new NotImplementedException();
        }
        public Task<IReadOnlyList<Guid>> SelectProblemVersionsAsync(ProblemDifficulty difficulty, int count) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Match>> ListByUserAsync(string userId) =>
            Task.FromResult<IReadOnlyList<Match>>(Matches);
    }
}
