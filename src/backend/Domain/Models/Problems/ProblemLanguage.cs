using Domain.Models.Common;

namespace Domain.Models.Problems;

/// <summary>Allowed runtime and starter code for a problem version.</summary>
public sealed class ProblemLanguage : Entity
{
    public Guid ProblemVersionId { get; set; }
    public int ProgrammingLanguageId { get; set; }
    public required string Template { get; set; }

    public ProblemVersion ProblemVersion { get; set; } = null!;
    public ProgrammingLanguage ProgrammingLanguage { get; set; } = null!;
}
