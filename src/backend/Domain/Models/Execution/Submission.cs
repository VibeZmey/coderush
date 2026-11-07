using Domain.Models.Common;
using Domain.Models.Matches;
using Domain.Models.Problems;

namespace Domain.Models.Execution;

/// <summary>Immutable attempt checked against the tests of MatchProblem.ProblemVersion.</summary>
public sealed class Submission : Entity
{
    public int ProgrammingLanguageId { get; set; }
    public Guid MatchProblemId { get; set; }
    public string? DuelPlayerId { get; set; }
    public required string SourceCode { get; set; }
    /// <summary>Server acceptance time determines deadline eligibility, not completion time.</summary>
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public ExecutionVerdict? Verdict { get; set; }
    public string? FailureCode { get; set; }

    public ProgrammingLanguage ProgrammingLanguage { get; set; } = null!;
    public MatchProblem MatchProblem { get; set; } = null!;
    public ICollection<SubmissionTestResult> TestResults { get; set; } = new List<SubmissionTestResult>();
}

