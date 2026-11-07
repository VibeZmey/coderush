using Domain.Models.Common;

namespace Domain.Models.Matches;

public abstract class Match : Entity
{
    public required string UserId { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Created;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public MatchFinishReason? FinishReason { get; set; }

    public ICollection<MatchProblem> Problems { get; set; } = new List<MatchProblem>();

    public virtual void Start(DateTimeOffset? now = null)
    {
        if (Status != MatchStatus.Created)
            throw new InvalidOperationException("Only a created match can be started.");

        StartedAt = (now ?? DateTimeOffset.UtcNow).ToUniversalTime();
        Status = MatchStatus.InProgress;
    }
    protected void Complete(DateTimeOffset finishedAt, DateTimeOffset completedAt, MatchFinishReason reason)
    {
        FinishedAt = finishedAt;
        CompletedAt = completedAt;
        FinishReason = reason;
        Status = MatchStatus.Completed;
    }
}
