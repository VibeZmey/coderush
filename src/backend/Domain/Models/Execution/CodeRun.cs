using Domain.Models.Common;
using Domain.Models.Matches;

namespace Domain.Models.Execution;

/// <summary>Run on player-supplied input. Does not award points or prove correctness.</summary>
public sealed class CodeRun : Entity
{
    public Guid MatchProblemId { get; set; }
    public string? DuelPlayerId { get; set; }
    public Guid ExecutionJobId { get; set; }

    public MatchProblem MatchProblem { get; set; } = null!;
    public ExecutionJob ExecutionJob { get; set; } = null!;
}
