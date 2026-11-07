using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Api.Validation;
using Domain.Models.Problems;
using Infrastructure.Persistence;

namespace Api.Endpoints;

public static class Duels
{
    public static void MapDuelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/duels").WithTags("Duels").RequireAuthorization();
        group.MapGet("/rating", (ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            service.RatingAsync(UserId(user), ct));
        group.MapPost("/state", (ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            service.StateAsync(UserId(user), ct));
        group.MapPost("/search", async (ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.SearchAsync(UserId(user), ct)));
        group.MapPost("/cancel", async (ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.CancelAsync(UserId(user), ct)));
        group.MapPost("/{id:guid}/accept", async (Guid id, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.AcceptAsync(id, UserId(user), ct)));
        group.MapPut("/{id:guid}/draft", async (Guid id, DuelDraftRequest request, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.SaveDraftAsync(id, UserId(user), request.ProgrammingLanguageId, request.SourceCode, ct)))
            .AddEndpointFilter<RequestValidationFilter<DuelDraftRequest>>();
        group.MapGet("/{id:guid}/opponent-code", async (Guid id, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            await service.OpponentCodeAsync(id, UserId(user), ct) is { } code ? Results.Ok(code) : Results.NotFound());
        group.MapGet("/{id:guid}/opponent-profile", async (Guid id, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            await service.OpponentProfileAsync(id, UserId(user), ct) is { } profile ? Results.Ok(profile) : Results.NotFound());
        group.MapPost("/custom", async (DuelLobbyRequest request, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.CreateAsync(UserId(user), request.DurationMinutes, request.Difficulty, ct)));
        group.MapPost("/custom/{id:guid}/join", async (Guid id, ClaimsPrincipal user, DuelService service, CancellationToken ct) =>
            Response(await service.JoinAsync(id, UserId(user), ct)));
    }

    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static IResult Response(DuelActionResult result) =>
        result.Success ? Results.NoContent() : Results.Conflict(new { message = result.Message });

    public sealed record DuelLobbyRequest(int DurationMinutes, ProblemDifficulty Difficulty);
    public sealed class DuelDraftRequest
    {
        public required int ProgrammingLanguageId { get; init; }
        [Required(AllowEmptyStrings = true), Utf8Text(65536)]
        public required string SourceCode { get; init; }
    }
}
