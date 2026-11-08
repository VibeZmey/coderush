namespace Domain.Models.Matches;

public enum MatchStatus
{
    Created = 0,
    InProgress = 1,
    Finalizing = 2,
    Completed = 3,
    Cancelled = 4
}

public enum MatchProblemStatus
{
    NotStarted = 0,
    Active = 1,
    Finalizing = 2,
    Completed = 3
}

public enum TrainingProblemOutcome
{
    Solved = 0,
    TimedOut = 1,
    Skipped = 2,
    Abandoned = 3
}

/// <summary>Why input was closed; kept separately from the outcome while submissions are pending.</summary>
public enum TrainingProblemCloseReason
{
    AcceptedSubmission = 0,
    TimeExpired = 1,
    Skipped = 2,
    SessionAbandoned = 3
}

public enum MatchType
{
    Training = 0,
    Duel = 1,
    Practice = 2
}

public enum MatchFinishReason
{
    Finished = 0,
    Abandoned = 1
}

