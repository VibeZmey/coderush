using System.Security.Claims;
using Api.Contracts.Matches;
using Api.Validation;
using Application.Common.Interfaces;
using Application.Features.Matches.CreateTrainingMatch;
using Application.Features.Matches.StartMatch;
using Application.Features.Matches.TrainingSession;

namespace Api.Endpoints;

public static class Match
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches");
        group.RequireAuthorization();
        group.MapGet("", async (ClaimsPrincipal user, IHandler<ListTrainingMatchesQuery, IReadOnlyList<TrainingMatchView>> handler) =>
            Results.Ok(await handler.HandleAsync(new(user.FindFirstValue(ClaimTypes.NameIdentifier)!))));
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IHandler<GetTrainingMatchQuery, TrainingSessionResult> handler) =>
            SessionResponse(await handler.HandleAsync(new(user.FindFirstValue(ClaimTypes.NameIdentifier)!, id))));
        group.MapPost("/{id:guid}/next", async (Guid id, ClaimsPrincipal user, IHandler<NextTrainingProblemCommand, TrainingSessionResult> handler) =>
            SessionResponse(await handler.HandleAsync(new(user.FindFirstValue(ClaimTypes.NameIdentifier)!, id))));
        group.MapPost("/{id:guid}/leave", async (Guid id, ClaimsPrincipal user, IHandler<LeaveTrainingMatchCommand, TrainingSessionResult> handler) =>
            SessionResponse(await handler.HandleAsync(new(user.FindFirstValue(ClaimTypes.NameIdentifier)!, id))));
        group.MapPut("/{id:guid}/draft", async (Guid id, ClaimsPrincipal user, SaveTrainingDraftRequest request,
            IHandler<SaveTrainingDraftCommand, TrainingSessionResult> handler) =>
                SessionResponse(await handler.HandleAsync(new(user.FindFirstValue(ClaimTypes.NameIdentifier)!, id,
                    request.MatchProblemId, request.ProgrammingLanguageId, request.SourceCode, request.StandardInput))))
            .AddEndpointFilter<RequestValidationFilter<SaveTrainingDraftRequest>>();
        group.MapPost("/training", CreateTrainingMatchAsync)
            .AddEndpointFilter<RequestValidationFilter<CreateTrainingMatchRequest>>()
            .Produces<CreateTrainingMatchResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/start", StartMatchAsync)
            .AddEndpointFilter<RequestValidationFilter<StartMatchRequest>>()
            .Produces<StartMatchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
        
    }

    private static async Task<IResult> StartMatchAsync(
        ClaimsPrincipal principal,
        StartMatchRequest request, 
        IHandler<StartMatchCommand, StartMatchResult> handler)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var command = new StartMatchCommand(userId, request.MatchId);
        var result = await handler.HandleAsync(command);

        return result.Outcome switch
        {
            StartMatchOutcome.Started when result.Match is { } match =>
                Results.Ok(new StartMatchResponse(match.Id, match.Status, match.StartedAt)),
            StartMatchOutcome.NotFound => Results.NotFound(),
            StartMatchOutcome.NotEnoughProblems => Results.Conflict(new
            {
                message = "Not enough published problems for the selected difficulty. Choose fewer problems or another difficulty."
            }),
            StartMatchOutcome.InvalidState => Results.Conflict(new
            {
                message = "Only a created match can be started."
            }),
            _ => throw new InvalidOperationException("Unexpected start-match result.")
        };
    }
    private static async Task<IResult> CreateTrainingMatchAsync(
        ClaimsPrincipal principal,
        CreateTrainingMatchRequest request,
        IHandler<CreateTrainingMatchCommand, CreateTrainingMatchResult> handler)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var command = new CreateTrainingMatchCommand(
            userId, request.Count, request.Difficulty, request.TimePerProblemSeconds);
            
        var result = await handler.HandleAsync(command);

        return Results.Created((string?)null,
            new CreateTrainingMatchResponse(result.Id, result.Status, result.CreatedAt));
    }

    private static IResult SessionResponse(TrainingSessionResult result) => result.Outcome switch
    {
        TrainingSessionOutcome.Success when result.Match is not null => Results.Ok(result.Match),
        TrainingSessionOutcome.Success => Results.NoContent(),
        TrainingSessionOutcome.NotFound => Results.NotFound(),
        TrainingSessionOutcome.LanguageUnavailable => Results.BadRequest(new { message = "This language is not available for the match problem." }),
        _ => Results.Conflict(new { message = "This action is not available for the current training state." })
    };
}
