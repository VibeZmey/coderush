namespace Infrastructure.Judge0;

public sealed class Judge0Options
{
    public string BaseUrl { get; set; } = "http://localhost:2358/";
    public string? AuthToken { get; set; }
    public int ProcessingTimeoutSeconds { get; set; } = 180;
}
