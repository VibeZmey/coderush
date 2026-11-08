using System.Security.Claims;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Api.Contracts.Auth;
using Api.Validation;
using Application.Common.Dto;
using Application.Features.Accounts.GetAccount;
using Application.Features.Accounts.Login;
using Application.Features.Accounts.Logout;
using Application.Features.Accounts.Register;

namespace Api.Endpoints;

public static class Auth
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");
        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<RequestValidationFilter<RegisterRequest>>().AllowAnonymous();
        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<RequestValidationFilter<LoginRequest>>().AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetMeAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, RegisterAccountHandler handler)
    {
        var result = await handler.HandleAsync(new RegisterAccountCommand(
            request.Email, request.Password, request.UserName));
        return result.Succeeded
            ? Results.Ok(result.Account)
            : Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, LoginHandler handler)
    {
        var status = await handler.HandleAsync(new LoginCommand(
            request.Email, request.Password, request.RememberMe));
        return status switch
        {
            LoginResult.Succeeded => Results.NoContent(),
            _ => Results.Unauthorized()
        };
    }

    private static async Task<IResult> LogoutAsync(LogoutHandler handler)
    {
        await handler.HandleAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal, IHandler<GetAccountQuery, AccountDto?> handler)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var account = await handler.HandleAsync(new GetAccountQuery(userId));
        return account is null ? Results.Unauthorized() : Results.Ok(new
        {
            account.Id, account.Email, account.UserName, IsAdmin = principal.IsInRole(UserRoles.Admin)
        });
    }
}
