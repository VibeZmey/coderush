using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Validation;

namespace Api.Contracts.Problems;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProblemTestCaseRequest
{
    [Required(AllowEmptyStrings = true), Utf8Text(1048576)]
    public required string Input { get; init; }

    [Required(AllowEmptyStrings = true), Utf8Text(1048576)]
    public required string ExpectedOutput { get; init; }

    public bool IsSample { get; init; }
}
