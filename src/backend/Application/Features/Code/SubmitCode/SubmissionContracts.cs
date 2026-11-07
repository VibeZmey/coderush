namespace Application.Features.Code.SubmitCode;

public sealed record SubmitCodeCommand(string UserId, Guid MatchProblemId, int ProgrammingLanguageId, string SourceCode);
public enum SubmitCodeOutcome { Accepted, NotFound, InvalidState, LanguageUnavailable, AlreadyChecking }
public sealed record SubmitCodeResult(SubmitCodeOutcome Outcome, SubmissionView? Submission = null);

// Hidden test input, expected output, stdout and stderr never enter this contract.
public sealed record SubmissionView(Guid Id, string Status, string? Verdict, int PassedTests, int TotalTests,
    int? FailedTestNumber, string? FailureCode, string? CompilationOutput, int? ExitCode, int? ExitSignal,
    SubmissionSampleFailure? Sample, DateTimeOffset SubmittedAt);
public sealed record SubmissionSampleFailure(string Input, string ExpectedOutput, string? StandardOutput, string? StandardError);

public interface ISubmissionService
{
    Task<SubmitCodeResult> SubmitAsync(SubmitCodeCommand command, CancellationToken ct);
    Task<SubmissionView?> GetAsync(Guid id, string userId, CancellationToken ct);
}
