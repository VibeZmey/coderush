using Domain.Models.Common;

namespace Domain.Models.Problems;

public sealed class Problem : Entity
{
    public required string Key { get; set; }
    public required string CreatedByUserId { get; set; }
    public bool IsPractice { get; init; }
    public bool IsArchived { get; set; }

    public ICollection<ProblemVersion> Versions { get; set; } = new List<ProblemVersion>();
}
