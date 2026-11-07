using System.Security.Claims;
using Api.Contracts.Code;
using Api.Validation;
using Application.Features.Code.SubmitCode;

namespace Api.Endpoints;

public static class Submissions
{
    public static void MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/code/submissions").WithTags("Submissions").RequireAuthorization();
        group.MapPost("", async (SubmitCodeRequest request, ClaimsPrincipal principal, ISubmissionService submissions, CancellationToken ct) =>
        {
            var result = await submissions.SubmitAsync(new(principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
                request.MatchProblemId, request.ProgrammingLanguageId, request.SourceCode), ct);
            return result.Outcome switch
            {
                SubmitCodeOutcome.Accepted => Results.Accepted($"/api/code/submissions/{result.Submission!.Id}", result.Submission),
                SubmitCodeOutcome.NotFound => Results.NotFound(),
                SubmitCodeOutcome.LanguageUnavailable => Results.Problem(statusCode: 400, title: "Language unavailable"),
                SubmitCodeOutcome.AlreadyChecking => Results.Conflict(new { message = "A solution for this task is already being checked." }),
                _ => Results.Conflict(new { message = "Отправка недоступна для текущего состояния матча." })
            };
        }).AddEndpointFilter<RequestValidationFilter<SubmitCodeRequest>>().Produces<SubmissionView>(202);
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ISubmissionService submissions, CancellationToken ct) =>
            await submissions.GetAsync(id, principal.FindFirstValue(ClaimTypes.NameIdentifier)!, ct) is { } submission
                ? Results.Ok(submission) : Results.NotFound()).Produces<SubmissionView>();
    }
}

