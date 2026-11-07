using Domain.Models.Problems;

namespace Application.Features.Matches.CreateTrainingMatch;

public sealed record CreateTrainingMatchCommand(
    string UserId,
    int Count,
    ProblemDifficulty Difficulty,
    int TimePerProblemSeconds);
