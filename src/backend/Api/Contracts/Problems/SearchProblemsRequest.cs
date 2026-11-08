using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Problems;

public sealed record SearchProblemsRequest(
    [property: StringLength(200)] string Search = "",
    [property: RegularExpression("Easy|Medium|Hard")] string? Difficulty = null,
    [property: RegularExpression("Draft|Published|Retired")] string? Status = null,
    [property: RegularExpression("newest|oldest|title|title-desc|difficulty|tests")] string Sort = "newest",
    [property: Range(1, 1000000)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 20,
    bool? IsPractice = null);
