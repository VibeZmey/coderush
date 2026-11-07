using System.Security.Claims;
using Api.Contracts.Problems;
using Api.Validation;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Application.Features.Problems.CreateProblem;
using Application.Features.Problems.ManageProblems;
using Domain.Models.Problems;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Application.Features.Problems.ManageProblems.ProblemDetails;

namespace Api.Endpoints;

public static class Problems
{
    public static void MapProblemEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/problems").WithTags("Problems").RequireAuthorization(UserRoles.Admin);
        admin.MapGet("/", SearchAsync)
            .AddEndpointFilter<RequestValidationFilter<SearchProblemsRequest>>()
            .Produces<ProblemPage>();
        admin.MapGet("/{id:guid}", GetAsync).Produces<ProblemDetails>().Produces(404);
        admin.MapPut("/{id:guid}", UpdateAsync)
            .AddEndpointFilter<RequestValidationFilter<UpdateProblemRequest>>()
            .Produces<CreateProblemResponse>().Produces(400).Produces(404).Produces(409);
        admin.MapPost("/import", ImportAsync)
            .AddEndpointFilter<RequestValidationFilter<ImportProblemsRequest>>()
            .WithMetadata(new RequestSizeLimitAttribute(16 * 1024 * 1024))
            .Produces(StatusCodes.Status201Created).Produces(400).Produces(409);
        app.MapPost("/api/problems", CreateProblemAsync)
            .WithTags("Problems")
            .RequireAuthorization(UserRoles.Admin)
            .AddEndpointFilter<RequestValidationFilter<CreateProblemRequest>>()
            .Produces<CreateProblemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> SearchAsync([AsParameters] SearchProblemsRequest request,
        IProblemManagementRepository repository, CancellationToken cancellationToken)
        => Results.Ok(await repository.SearchAsync(new ProblemSearch(request.Search,
            request.Difficulty is { Length: > 0 } d ? Enum.Parse<ProblemDifficulty>(d) : null,
            request.Status is { Length: > 0 } s ? Enum.Parse<PublicationStatus>(s) : null,
            request.Sort, request.Page, request.PageSize), cancellationToken));

    private static async Task<IResult> GetAsync(Guid id, IProblemManagementRepository repository, CancellationToken cancellationToken)
        => await repository.GetAsync(id, cancellationToken) is { } problem ? Results.Ok(problem) : Results.NotFound();

    private static async Task<IResult> UpdateAsync(Guid id, UpdateProblemRequest request, ClaimsPrincipal principal,
        ProblemManagementHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.UpdateAsync(id, request.BaseVersionId,
            ToCommand(request.Problem, principal.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken);
        return result.Outcome == ProblemWriteOutcome.Saved && result.Problem is { } p
            ? Results.Ok(new CreateProblemResponse(p.Id, p.VersionId, p.VersionNumber, p.Status, p.CreatedAt))
            : WriteFailure(result.Outcome);
    }

    private static async Task<IResult> ImportAsync(ImportProblemsRequest request, ClaimsPrincipal principal,
        ProblemManagementHandler handler, CancellationToken cancellationToken)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await handler.ImportAsync(request.Problems.Select(p => ToCommand(p, userId)).ToArray(), cancellationToken);
        return result.Outcome == ProblemWriteOutcome.Saved
            ? Results.Json(new { problems = result.Problems!.Select(p => new CreateProblemResponse(p.Id, p.VersionId, p.VersionNumber, p.Status, p.CreatedAt)) }, statusCode: 201)
            : WriteFailure(result.Outcome);
    }

    private static IResult WriteFailure(ProblemWriteOutcome outcome) => outcome switch
    {
        ProblemWriteOutcome.NotFound => Results.NotFound(),
        ProblemWriteOutcome.DuplicateKey => Results.Conflict(new { message = "Задача с таким ключом уже существует или повторяется в файле. Изменения не сохранены." }),
        ProblemWriteOutcome.Conflict => Results.Conflict(new { message = "Задача была изменена другим запросом. Обновите её и повторите изменения." }),
        ProblemWriteOutcome.LanguageUnavailable => Results.Problem(statusCode: 400, detail: "Выберите существующие включённые языки без повторений."),
        _ => throw new InvalidOperationException("Unexpected problem write result.")
    };

    private static CreateProblemCommand ToCommand(CreateProblemRequest request, string userId) => new(
        userId, request.Key, request.Title, request.DescriptionMarkdown, request.InputDescriptionMarkdown,
        request.OutputDescriptionMarkdown, request.ConstraintsMarkdown, request.ExplanationMarkdown,
        request.Difficulty, request.Publish, request.CpuTimeLimitSeconds, request.WallTimeLimitSeconds,
        request.MemoryLimitKilobytes, request.OutputLimitKilobytes,
        request.Languages.Select(l => new ProblemLanguageSettings(l.ProgrammingLanguageId, l.Template)).ToArray(),
        request.TestCases.Select(t => new ProblemTestCaseData(t.Input, t.ExpectedOutput, t.IsSample)).ToArray());

    private static async Task<IResult> CreateProblemAsync(
        ClaimsPrincipal principal,
        CreateProblemRequest request,
        IHandler<CreateProblemCommand, CreateProblemResult> handler)
    {
        var command = new CreateProblemCommand(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
            request.Key, request.Title, request.DescriptionMarkdown,
            request.InputDescriptionMarkdown, request.OutputDescriptionMarkdown,
            request.ConstraintsMarkdown, request.ExplanationMarkdown, request.Difficulty, request.Publish,
            request.CpuTimeLimitSeconds, request.WallTimeLimitSeconds,
            request.MemoryLimitKilobytes, request.OutputLimitKilobytes,
            request.Languages.Select(language => new ProblemLanguageSettings(
                language.ProgrammingLanguageId, language.Template)).ToArray(),
            request.TestCases.Select(test => new ProblemTestCaseData(
                test.Input, test.ExpectedOutput, test.IsSample)).ToArray());
        var result = await handler.HandleAsync(command);

        return result.Outcome switch
        {
            CreateProblemOutcome.Created when result.Problem is { } problem => Results.Created((string?)null,
                new CreateProblemResponse(problem.Id, problem.VersionId, problem.VersionNumber, problem.Status, problem.CreatedAt)),
            CreateProblemOutcome.DuplicateKey => Results.Conflict(new { message = "A problem with this key already exists." }),
            CreateProblemOutcome.LanguageUnavailable => Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid languages", detail: "Select existing, enabled languages without duplicates."),
            _ => throw new InvalidOperationException("Unexpected create-problem result.")
        };
    }
}
