using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Execution;

namespace Infrastructure.Judge0;

public sealed class Judge0Client(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<string> DispatchAsync(ExecutionJob job, CancellationToken ct, string? expectedOutput = null)
    {
        var payload = new
        {
            source_code = Encode(job.SourceCode), language_id = job.ProgrammingLanguageId,
            stdin = Encode(job.StandardInput), cpu_time_limit = job.CpuTimeLimitSeconds,
            expected_output = expectedOutput is null ? null : Encode(expectedOutput),
            wall_time_limit = job.WallTimeLimitSeconds, memory_limit = job.MemoryLimitKilobytes,
            max_file_size = job.OutputLimitKilobytes, enable_network = false
        };
        // Judge0's Rails/Puma version does not read chunked JSON request bodies reliably.
        // Buffer the JSON so HttpClient sends Content-Length rather than chunked encoding.
        using var request = new HttpRequestMessage(HttpMethod.Post, "submissions?base64_encoded=true&wait=false")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Judge0 rejected dispatch: HTTP {(int)response.StatusCode}. {error[..Math.Min(error.Length, 4096)]}",
                null, response.StatusCode);
        }
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, ct);
        return !string.IsNullOrWhiteSpace(body?.Token) ? body.Token : throw new JsonException("Missing execution token.");
    }

    public async Task<Judge0Response> PollAsync(string token, CancellationToken ct)
    {
        using var response = await http.GetAsync($"submissions/{Uri.EscapeDataString(token)}?base64_encoded=true&fields=status,stdout,stderr,compile_output,exit_code,exit_signal,time,wall_time,memory", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Judge0Response>(Json, ct)
            ?? throw new JsonException("Missing execution result.");
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    private sealed record TokenResponse(string Token);
}

public sealed class Judge0Response
{
    public Judge0Status? Status { get; set; }
    public string? Stdout { get; set; }
    public string? Stderr { get; set; }
    [JsonPropertyName("compile_output")] public string? CompileOutput { get; set; }
    [JsonPropertyName("exit_code")] public int? ExitCode { get; set; }
    [JsonPropertyName("exit_signal")] public int? ExitSignal { get; set; }
    public double? Time { get; set; }
    [JsonPropertyName("wall_time")] public double? WallTime { get; set; }
    public long? Memory { get; set; }

    public ExecutionResult ToResult(bool checking = false) => new()
    {
        Verdict = Status?.Id switch
        {
            3 => checking ? ExecutionVerdict.Accepted : ExecutionVerdict.Succeeded,
            4 => ExecutionVerdict.WrongAnswer,
            5 => ExecutionVerdict.TimeLimitExceeded,
            6 => ExecutionVerdict.CompilationError,
            >= 7 and <= 12 => ExecutionVerdict.RuntimeError,
            _ => throw new InvalidOperationException("Unexpected Judge0 verdict.")
        },
        StandardOutput = Decode(Stdout), StandardError = Decode(Stderr), CompilationOutput = Decode(CompileOutput),
        ExitCode = ExitCode, ExitSignal = ExitSignal, CpuTimeSeconds = Time,
        WallTimeSeconds = WallTime, MemoryKilobytes = Memory
    };

    private static string? Decode(string? value) => value is null ? null : Encoding.UTF8.GetString(Convert.FromBase64String(value));
}

public sealed record Judge0Status(int Id);
