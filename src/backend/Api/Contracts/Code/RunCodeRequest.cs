using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Contracts.Code;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RunCodeRequest
{
    public required Guid MatchProblemId { get; init; }
    public required int ProgrammingLanguageId { get; init; }

    [Required, Utf8Text(65536)]
    public required string SourceCode { get; init; }

    [Required(AllowEmptyStrings = true), Utf8Text(1048576)]
    public string StandardInput { get; init; } = string.Empty;
}
