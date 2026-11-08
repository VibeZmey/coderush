using System.Security.Claims;
using Api.Authorization;
using Api.Endpoints;
using Application;
using Application.Common.Constants;
using Application.Features.Problems.ManageProblems;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Problems;

public sealed class AdminAuthorizationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, true)]
    public async Task AdminPolicyRequiresLoginUserIdAndRole(bool authenticated, bool hasUserId, bool admin, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiAuthorization();
        await using var provider = services.BuildServiceProvider();
        var identity = new ClaimsIdentity(authenticated ? "test" : null);
        if (hasUserId)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user"));
        if (admin)
            identity.AddClaim(new Claim(ClaimTypes.Role, UserRoles.Admin));

        var result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(identity), null, UserRoles.Admin);

        Assert.Equal(allowed, result.Succeeded);
    }

    [Fact]
    public async Task CreationEndpointExplicitlyRequiresAdminPolicy()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production", ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddApplication();
        builder.Services.AddScoped<IProblemManagementRepository, ProblemManagementRepository>();
        builder.Services.AddApiAuthorization();
        await using var app = builder.Build();
        app.MapProblemEndpoints();
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText == "/api/problems");

        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(), data => data.Policy == UserRoles.Admin);
    }
}
