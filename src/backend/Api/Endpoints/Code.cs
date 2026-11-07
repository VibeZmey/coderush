using System.Security.Claims;
using Api.Contracts.Code;
using Api.Validation;
using Application.Common.Interfaces;
using Application.Features.Code.RunCode;

namespace Api.Endpoints;

public static class Code
{
    public static void MapRunCodeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/code/runs/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            ICodeRunReader runs, CancellationToken cancellationToken) =>
        {
            var run = await runs.GetAsync(id, principal.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);
            return run is null ? Results.NotFound() : Results.Ok(CodeRunDetailsResponse.From(run));
        }).WithTags("Code runs").RequireAuthorization()
            .Produces<CodeRunDetailsResponse>().Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/code/run", RunCodeAsync)
            .AddEndpointFilter<RequestValidationFilter<RunCodeRequest>>()
            .WithTags("Code runs")
            .RequireAuthorization()
            .Produces<RunCodeResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RunCodeAsync(
        ClaimsPrincipal principal,
        RunCodeRequest request,
        IHandler<RunCodeCommand, RunCodeResult> handler)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await handler.HandleAsync(new RunCodeCommand(
            userId, request.MatchProblemId, request.ProgrammingLanguageId,
            request.SourceCode, request.StandardInput));

        return result.Outcome switch
        {
            RunCodeOutcome.Accepted when result.Run is { } run =>
                Results.Accepted($"/api/code/runs/{run.Id}", new RunCodeResponse(run.Id, run.Status, run.CreatedAt)),
            RunCodeOutcome.LanguageUnavailable => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Language unavailable",
                detail: "This language is not available for the match problem."),
            RunCodeOutcome.NotFound => Results.NotFound(),
            RunCodeOutcome.InvalidState => Results.Conflict(new
            {
                message = "Code can only be run for an active match problem before its deadline."
            }),
            _ => throw new InvalidOperationException("Unexpected run-code result.")
        };
    }
}
