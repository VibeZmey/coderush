using System.Security.Claims;
using Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services) =>
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(ClaimTypes.NameIdentifier)
                .Build();
            options.AddPolicy(UserRoles.Admin, new AuthorizationPolicyBuilder(options.DefaultPolicy)
                .RequireRole(UserRoles.Admin).Build());
        });
}
