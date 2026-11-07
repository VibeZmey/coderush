using Application.Common.Interfaces;
using Domain.Models.Matches;

namespace Application.Features.Matches.CreateTrainingMatch;

public sealed class CreateTrainingMatchHandler(IMatchRepository matches)
    : IHandler<CreateTrainingMatchCommand, CreateTrainingMatchResult>
{
    public async Task<CreateTrainingMatchResult> HandleAsync(CreateTrainingMatchCommand request)
    {
        var match = new TrainingMatch
        {
            UserId = request.UserId,
            Count = request.Count,
            Difficulty = request.Difficulty,
            TimePerProblemSeconds = request.TimePerProblemSeconds
        };

        await matches.AddAsync(match);
        return new CreateTrainingMatchResult(match.Id, match.Status, match.CreatedAt);
    }
}
