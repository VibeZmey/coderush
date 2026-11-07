using System.Text.Json.Serialization;
using Application.Features.Code.RunCode;
using Domain.Models.Execution;

namespace Api.Contracts.Code;

public sealed record CodeRunDetailsResponse(Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProcessingStatus>))] ProcessingStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, string? FailureCode, CodeRunOutput? Result)
{
    public static CodeRunDetailsResponse From(CodeRunView run) => new(run.Id, run.Status, run.CreatedAt,
        run.CompletedAt, run.FailureCode, run.Result is { } r ? new(r.Verdict, r.StandardOutput,
            r.StandardError, r.CompilationOutput, r.ExitCode, r.ExitSignal, r.CpuTimeSeconds,
            r.WallTimeSeconds, r.MemoryKilobytes) : null);
}

public sealed record CodeRunOutput(
    [property: JsonConverter(typeof(JsonStringEnumConverter<ExecutionVerdict>))] ExecutionVerdict Verdict,
    string? StandardOutput, string? StandardError, string? CompilationOutput, int? ExitCode,
    int? ExitSignal, double? CpuTimeSeconds, double? WallTimeSeconds, long? MemoryKilobytes);
