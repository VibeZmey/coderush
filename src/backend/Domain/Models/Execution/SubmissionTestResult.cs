using Domain.Models.Common;
using Domain.Models.Problems;

namespace Domain.Models.Execution;

public sealed class SubmissionTestResult : Entity
{
    public Guid SubmissionId { get; set; }
    public Guid ProblemTestCaseId { get; set; }
    /// <summary>Current attempt; earlier attempts remain linked through ExecutionJob.RetryOfJobId.</summary>
    public Guid ExecutionJobId { get; set; }
    public ExecutionVerdict? Verdict { get; set; }

    public Submission Submission { get; set; } = null!;
    public ProblemTestCase ProblemTestCase { get; set; } = null!;
    public ExecutionJob ExecutionJob { get; set; } = null!;
}
