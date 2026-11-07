using Domain.Models.Problems;

namespace Domain.Models.Execution;

public sealed class DuelDraft
{
    public Guid MatchId { get; set; }
    public required string UserId { get; set; }
    public int ProgrammingLanguageId { get; set; }
    public required string SourceCode { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ProgrammingLanguage ProgrammingLanguage { get; set; } = null!;
}
