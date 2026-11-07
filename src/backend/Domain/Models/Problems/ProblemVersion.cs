using Domain.Models.Common;

namespace Domain.Models.Problems;

/// <summary>A published version must remain immutable once used by a match or submission.</summary>
public sealed class ProblemVersion : Entity
{
    public Guid ProblemId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public required string Title { get; set; }
    public required string DescriptionMarkdown { get; set; }
    public string? InputDescriptionMarkdown { get; set; }
    public string? OutputDescriptionMarkdown { get; set; }
    public string? ConstraintsMarkdown { get; set; }
    public string? ExplanationMarkdown { get; set; }
    public ProblemDifficulty Difficulty { get; set; } = ProblemDifficulty.Unspecified;
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public required string CreatedByUserId { get; set; }
    public double CpuTimeLimitSeconds { get; set; } = 2;
    public double WallTimeLimitSeconds { get; set; } = 10;
    public int MemoryLimitKilobytes { get; set; } = 128000;
    public int OutputLimitKilobytes { get; set; } = 1024;

    public Problem Problem { get; set; } = null!;
    public ICollection<ProblemLanguage> Languages { get; set; } = new List<ProblemLanguage>();
    public ICollection<ProblemTestCase> TestCases { get; set; } = new List<ProblemTestCase>();
}
