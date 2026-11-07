using Domain.Models.Execution;

namespace Domain.Models.Matches;

public sealed class DuelMatch : Match
{
    public string? OpponentId { get; set; }
    public bool Ranked { get; set; }
    public int FirstRating { get; set; }
    public int SecondRating { get; set; }
    public int DurationSeconds { get; set; }
    public DateTimeOffset? FirstSeenAt { get; set; }
    public DateTimeOffset? SecondSeenAt { get; set; }
    public DateTimeOffset? AcceptBy { get; set; }
    public bool FirstAccepted { get; set; }
    public bool SecondAccepted { get; set; }
    public DuelOutcome? Outcome { get; set; }
    public int FirstDelta { get; set; }
    public int SecondDelta { get; set; }

    public bool HasPlayer(string userId) => UserId == userId || OpponentId == userId;

    public void Offer(string opponentId, int opponentRating, DateTimeOffset now)
    {
        OpponentId = opponentId;
        SecondRating = opponentRating;
        SecondSeenAt = now;
        AcceptBy = now.AddSeconds(30);
    }

    public bool Accept(string userId, DateTimeOffset now)
    {
        if (!HasPlayer(userId) || AcceptBy is null || Status == MatchStatus.Cancelled)
            return false;
        if (Status != MatchStatus.Created)
            return true;
        if (now >= AcceptBy)
            return false;

        if (UserId == userId) FirstAccepted = true;
        else SecondAccepted = true;
        if (!FirstAccepted || !SecondAccepted)
            return true;

        var start = now + DuelRules.Countdown;
        Start(start);
        Problems.Single().Activate(start, DurationSeconds);
        return true;
    }

    public bool ExpireOffer(DateTimeOffset now)
    {
        if (Status == MatchStatus.Created && AcceptBy <= now)
        {
            Status = MatchStatus.Cancelled;
            CompletedAt = now;
            return true;
        }
        return false;
    }

    public void Refresh(DateTimeOffset now)
    {
        if (Status is not (MatchStatus.InProgress or MatchStatus.Finalizing))
            return;

        var problem = Problems.Single();
        var deadline = problem.SubmissionDeadlineAt!.Value;
        var closed = now >= deadline || problem.Submissions.Any(x =>
            x.SubmittedAt >= StartedAt && x.SubmittedAt < deadline && DuelRules.IsAccepted(x));
        if (!closed)
            return;

        FinishedAt ??= now < deadline ? now : deadline;
        Status = MatchStatus.Finalizing;
        var result = DuelRules.Resolve(
            problem.Submissions.Where(x => x.DuelPlayerId == UserId),
            problem.Submissions.Where(x => x.DuelPlayerId == OpponentId),
            StartedAt!.Value, deadline, now, Ranked);
        if (result is null)
            return;

        Outcome = result.Outcome;
        FirstDelta = result.FirstPlayerDelta;
        SecondDelta = result.SecondPlayerDelta;
        Complete(FinishedAt.Value, now, MatchFinishReason.Finished);
    }
}

public sealed class DuelQueueEntry
{
    public required string UserId { get; set; }
    public DateTimeOffset EnqueuedAt { get; set; }
    public DateTimeOffset SeenAt { get; set; }
}
