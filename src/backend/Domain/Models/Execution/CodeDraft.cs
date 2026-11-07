using Domain.Models.Common;
using Domain.Models.Matches;
using Domain.Models.Problems;

namespace Domain.Models.Execution;

/// <summary>Saved editor state for a training task and runtime. Owner is MatchProblem.Match.UserId.</summary>
public sealed class CodeDraft : Entity
{
    public int ProgrammingLanguageId { get; set; }
    public Guid MatchProblemId { get; set; }
    public required string SourceCode { get; set; }
    public string CustomInput { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ProgrammingLanguage ProgrammingLanguage { get; set; } = null!;
    public MatchProblem MatchProblem { get; set; } = null!;
}
