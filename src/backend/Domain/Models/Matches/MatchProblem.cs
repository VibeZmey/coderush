using Domain.Models.Common;
using Domain.Models.Execution;
using Domain.Models.Problems;

namespace Domain.Models.Matches;

/// <summary>Assignment and durable player result for one task in a training session.</summary>
public sealed class MatchProblem : Entity
{
    public Guid MatchId { get; set; }
    public Guid ProblemVersionId { get; set; }
    public int Position { get; set; }
    public MatchProblemStatus Status { get; set; } = MatchProblemStatus.NotStarted;
    public DateTimeOffset? StartedAt { get; set; }
    /// <summary>StartedAt plus this training session's TimePerProblemSeconds.</summary>
    public DateTimeOffset? SubmissionDeadlineAt { get; set; }
    /// <summary>Server time at which further submissions were stopped (solve, skip, timeout or exit).</summary>
    public DateTimeOffset? ClosedAt { get; set; }
    public TrainingProblemCloseReason? CloseReason { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    /// <summary>Null until all eligible submissions have been processed successfully.</summary>
    public TrainingProblemOutcome? Outcome { get; set; }
    /// <summary>The accepted submission with the earliest SubmittedAt, regardless of processing order.</summary>
    public Guid? FirstAcceptedSubmissionId { get; set; }

    public Match Match { get; set; } = null!;
    public ProblemVersion ProblemVersion { get; set; } = null!;
    public Submission? FirstAcceptedSubmission { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<CodeDraft> Drafts { get; set; } = new List<CodeDraft>();
    public ICollection<CodeRun> CodeRuns { get; set; } = new List<CodeRun>();

    public bool IsSubmissionOpen(DateTimeOffset now) =>
        Match is PracticeMatch || SubmissionDeadlineAt > now;

    public void Activate(DateTimeOffset now, int durationSeconds)
    {
        if (Status != MatchProblemStatus.NotStarted)
            throw new InvalidOperationException("Only an unopened task can be activated.");
        StartedAt = now;
        SubmissionDeadlineAt = now.AddSeconds(durationSeconds);
        Status = MatchProblemStatus.Active;
    }

    public void Close(TrainingProblemOutcome outcome, TrainingProblemCloseReason reason,
        DateTimeOffset closedAt, DateTimeOffset finalizedAt)
    {
        if (Status is MatchProblemStatus.Completed or MatchProblemStatus.Finalizing)
            return;
        CloseReason = reason;
        ClosedAt = closedAt;
        if (Submissions.Any(submission => submission.Status is ProcessingStatus.Pending or ProcessingStatus.Queued or ProcessingStatus.Running))
        {
            Status = MatchProblemStatus.Finalizing;
            return;
        }
        Outcome = outcome;
        FinalizedAt = finalizedAt;
        Status = MatchProblemStatus.Completed;
    }

    public bool ResolveSubmissions(DateTimeOffset now)
    {
        var accepted = Submissions.Where(s => s.Verdict == ExecutionVerdict.Accepted && s.Status == ProcessingStatus.Completed &&
            s.SubmittedAt >= StartedAt && s.SubmittedAt < SubmissionDeadlineAt && (ClosedAt == null || s.SubmittedAt <= ClosedAt))
            .OrderBy(s => s.SubmittedAt).ThenBy(s => s.Id).FirstOrDefault();
        if (accepted is not null && (Outcome != TrainingProblemOutcome.Solved || FirstAcceptedSubmissionId != accepted.Id))
        {
            FirstAcceptedSubmissionId = accepted.Id;
            Outcome = TrainingProblemOutcome.Solved;
            CloseReason = TrainingProblemCloseReason.AcceptedSubmission;
            ClosedAt = accepted.SubmittedAt;
            FinalizedAt = now;
            Status = MatchProblemStatus.Completed;
            return true;
        }
        if (Status == MatchProblemStatus.Completed && Outcome != TrainingProblemOutcome.Solved && Submissions.Any(s =>
            s.SubmittedAt < SubmissionDeadlineAt && (ClosedAt == null || s.SubmittedAt <= ClosedAt) &&
            s.Status is ProcessingStatus.Pending or ProcessingStatus.Queued or ProcessingStatus.Running))
        {
            Status = MatchProblemStatus.Finalizing;
            Outcome = null;
            FinalizedAt = null;
            return true;
        }
        if (Status == MatchProblemStatus.Finalizing && !Submissions.Any(s =>
            s.Status is ProcessingStatus.Pending or ProcessingStatus.Queued or ProcessingStatus.Running))
        {
            Outcome = CloseReason switch
            {
                TrainingProblemCloseReason.Skipped => TrainingProblemOutcome.Skipped,
                TrainingProblemCloseReason.SessionAbandoned => TrainingProblemOutcome.Abandoned,
                _ => TrainingProblemOutcome.TimedOut
            };
            FinalizedAt = now;
            Status = MatchProblemStatus.Completed;
            return true;
        }
        return false;
    }
}
