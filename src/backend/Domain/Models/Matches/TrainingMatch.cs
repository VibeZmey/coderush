
using Domain.Models.Problems;

namespace Domain.Models.Matches;

/// <summary>A single-player session. Every task receives its own full time allowance.</summary>
public sealed class TrainingMatch : Match
{
    public int Count { get; set; } = 2;
    public ProblemDifficulty Difficulty { get; set; } = ProblemDifficulty.Easy;
    public int TimePerProblemSeconds { get; set; } = 300;

    public override void Start(DateTimeOffset? now = null)
    {
        if (Problems.Count != Count)
            throw new InvalidOperationException("All training tasks must be assigned before starting.");
        var startedAt = (now ?? DateTimeOffset.UtcNow).ToUniversalTime();
        base.Start(startedAt);
        Problems.OrderBy(problem => problem.Position).First().Activate(startedAt, TimePerProblemSeconds);
    }

    public bool Refresh(DateTimeOffset now)
    {
        if (Status is not (MatchStatus.InProgress or MatchStatus.Finalizing or MatchStatus.Completed))
            return false;
        var changed = false;
        foreach (var problem in Problems)
            changed |= problem.ResolveSubmissions(now);
        var active = Problems.SingleOrDefault(problem => problem.Status == MatchProblemStatus.Active);
        if (active?.SubmissionDeadlineAt is { } deadline && now >= deadline)
        {
            active.Close(TrainingProblemOutcome.TimedOut, TrainingProblemCloseReason.TimeExpired, deadline, now);
            changed = true;
        }
        if (Problems.Count > 0 && Problems.All(problem => problem.Status == MatchProblemStatus.Completed) &&
            (Status != MatchStatus.Completed || changed))
        {
            Complete(FinishReason == MatchFinishReason.Abandoned && FinishedAt is { } abandonedAt
                ? abandonedAt : Problems.Max(problem => problem.ClosedAt)!.Value, now, FinishReason ?? MatchFinishReason.Finished);
            changed = true;
        }
        else if (Problems.Any(p => p.Status == MatchProblemStatus.Finalizing) &&
            Problems.All(p => p.Status is MatchProblemStatus.Completed or MatchProblemStatus.Finalizing))
        {
            Status = MatchStatus.Finalizing;
            changed = true;
        }
        return changed;
    }

    public bool MoveNext(DateTimeOffset now)
    {
        Refresh(now);
        if (Status != MatchStatus.InProgress)
            return false;
        Problems.SingleOrDefault(problem => problem.Status == MatchProblemStatus.Active)?
            .Close(TrainingProblemOutcome.Skipped, TrainingProblemCloseReason.Skipped, now, now);
        var next = Problems.OrderBy(problem => problem.Position)
            .FirstOrDefault(problem => problem.Status == MatchProblemStatus.NotStarted);
        if (next is not null)
            next.Activate(now, TimePerProblemSeconds);
        else
            Refresh(now);
        return true;
    }

    public void Leave(DateTimeOffset now)
    {
        Refresh(now);
        if (Status != MatchStatus.InProgress)
            return;
        foreach (var problem in Problems.Where(problem => problem.Status != MatchProblemStatus.Completed))
            problem.Close(TrainingProblemOutcome.Abandoned, TrainingProblemCloseReason.SessionAbandoned, now, now);
        if (Problems.Any(p => p.Status == MatchProblemStatus.Finalizing))
        {
            FinishedAt = now;
            FinishReason = MatchFinishReason.Abandoned;
            Status = MatchStatus.Finalizing;
        }
        else Complete(now, now, MatchFinishReason.Abandoned);
    }
}
