using Domain.Models.Common;

namespace Domain.Models.Problems;

/// <summary>Administrative data; hidden inputs and expected outputs must not be exposed to players.</summary>
public sealed class ProblemTestCase : Entity
{
    public Guid ProblemVersionId { get; set; }
    public int Position { get; set; }
    public required string Input { get; set; }
    public required string ExpectedOutput { get; set; }
    public bool IsSample { get; set; }

    public ProblemVersion ProblemVersion { get; set; } = null!;
}
