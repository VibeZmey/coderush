using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Contracts.Problems;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProblemRequest : IValidatableObject
{
    public required Guid BaseVersionId { get; init; }
    [Required] public required CreateProblemRequest Problem { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BaseVersionId == Guid.Empty) yield return new ValidationResult("Укажите версию редактируемой задачи.");
    }
}
