using Application.Common.Interfaces;
using Domain.Models.Matches;

namespace Application.Features.Matches.StartMatch;

public sealed class StartMatchHandler : IHandler<StartMatchCommand, StartMatchResult>
{
    private readonly IMatchRepository _matchRepository;

    public StartMatchHandler(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task<StartMatchResult> HandleAsync(StartMatchCommand command)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId);

        if (match is null || match.UserId != command.UserId)
        {
            return new(StartMatchOutcome.NotFound);
        }

        if (match.Status != MatchStatus.Created)
        {
            return new(StartMatchOutcome.InvalidState);
        }

        if (match is not TrainingMatch training)
            return new(StartMatchOutcome.InvalidState);
        var versions = await _matchRepository.SelectProblemVersionsAsync(training.Difficulty, training.Count);
        if (versions.Count != training.Count)
            return new(StartMatchOutcome.NotEnoughProblems);
        training.Problems = versions.Select((versionId, index) => new MatchProblem
        {
            MatchId = match.Id, ProblemVersionId = versionId, Position = index + 1
        }).ToList();
        training.Start();
        await _matchRepository.UpdateAsync(match);
        return new(StartMatchOutcome.Started, new StartedMatch(match.Id, match.Status, match.StartedAt!.Value));
    }
}
