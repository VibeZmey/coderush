namespace Domain.Models.Execution;

/// <summary>Value object for a completed execution; infrastructure failures belong to the job.</summary>
public sealed class ExecutionResult
{
    public ExecutionVerdict Verdict { get; set; }
    public string? StandardOutput { get; set; }
    public string? StandardError { get; set; }
    public string? CompilationOutput { get; set; }
    public string? DiagnosticMessage { get; set; }
    public int? ExitCode { get; set; }
    public int? ExitSignal { get; set; }
    public double? CpuTimeSeconds { get; set; }
    public double? WallTimeSeconds { get; set; }
    public long? MemoryKilobytes { get; set; }
}
