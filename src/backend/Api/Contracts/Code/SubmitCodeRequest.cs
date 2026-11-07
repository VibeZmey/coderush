using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Validation;

namespace Api.Contracts.Code;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SubmitCodeRequest
{
    public required Guid MatchProblemId { get; init; }
    public required int ProgrammingLanguageId { get; init; }
    [Required, Utf8Text(65536)]
    public required string SourceCode { get; init; }
}
