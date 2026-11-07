using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Validation;

namespace Api.Contracts.Problems;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProblemLanguageRequest
{
    public required int ProgrammingLanguageId { get; init; }

    [Required(AllowEmptyStrings = true), Utf8Text(65536)]
    public string Template { get; init; } = string.Empty;

}
