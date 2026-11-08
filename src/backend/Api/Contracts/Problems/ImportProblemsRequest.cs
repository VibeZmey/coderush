using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Contracts.Problems;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ImportProblemsRequest
{
    [Required, MinLength(1), MaxLength(60)]
    public required List<CreateProblemRequest> Problems { get; init; }
}
