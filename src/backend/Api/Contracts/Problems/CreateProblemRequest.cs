using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Validation;
using Domain.Models.Problems;

namespace Api.Contracts.Problems;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProblemRequest : IValidatableObject
{
    [Required, StringLength(128), RegularExpression("[a-z0-9]+(?:-[a-z0-9]+)*")]
    public required string Key { get; init; }

    [Required, StringLength(256), Utf8Text(1024)]
    public required string Title { get; init; }

    [Required, Utf8Text(1048576)]
    public required string DescriptionMarkdown { get; init; }

    [Utf8Text(1048576)]
    public string? InputDescriptionMarkdown { get; init; }

    [Utf8Text(1048576)]
    public string? OutputDescriptionMarkdown { get; init; }

    [Utf8Text(1048576)]
    public string? ConstraintsMarkdown { get; init; }

    [Utf8Text(1048576)]
    public string? ExplanationMarkdown { get; init; }

    [Range(1, 3), JsonConverter(typeof(JsonStringEnumConverter<ProblemDifficulty>))]
    public required ProblemDifficulty Difficulty { get; init; }

    public bool Publish { get; init; }
    public bool IsPractice { get; init; }
    public string[]? Topics { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsPractice && (Topics is null || Topics.Length == 0))
            yield return new("Укажите хотя бы одну тему для каталога.", [nameof(Topics)]);
        else if (Topics is { } topics && (topics.Length > 10 ||
            topics.Any(topic => string.IsNullOrWhiteSpace(topic) || topic.Trim().Length > 40 || topic.Any(char.IsControl))))
            yield return new("Укажите до 10 тем длиной от 1 до 40 символов.", [nameof(Topics)]);
    }

    [Range(double.Epsilon, double.MaxValue)]
    public double CpuTimeLimitSeconds { get; init; } = 2;

    [Range(double.Epsilon, double.MaxValue)]
    public double WallTimeLimitSeconds { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int MemoryLimitKilobytes { get; init; } = 128000;

    [Range(1, int.MaxValue)]
    public int OutputLimitKilobytes { get; init; } = 1024;

    [Required, MinLength(1)]
    public required List<CreateProblemLanguageRequest> Languages { get; init; }

    [Required, MinLength(1)]
    public required List<CreateProblemTestCaseRequest> TestCases { get; init; }
}
