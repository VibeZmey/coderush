using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Validation;

namespace Api.Contracts.Matches;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SaveTrainingDraftRequest
{
    public required Guid MatchProblemId { get; init; }
    public required int ProgrammingLanguageId { get; init; }
    [Required(AllowEmptyStrings = true), Utf8Text(65536)]
    public required string SourceCode { get; init; }
    [Required(AllowEmptyStrings = true), Utf8Text(1048576)]
    public string StandardInput { get; init; } = "";
}
