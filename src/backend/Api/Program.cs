using Api.Endpoints;
using Api.Profile;
using Microsoft.AspNetCore.RateLimiting;
using Api.Authorization;
using Api.Security;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiAuthorization();
builder.Services.AddSingleton<ProfileMediaStorage>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter(ProfileMediaEndpoints.UploadPolicy, limiter => limiter.PermitLimit = 2);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CodeRush API v1");
        options.RoutePrefix = "swagger";
    });
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Docker's frontend terminates TLS; cookie security remains Secure + HttpOnly.
if (!builder.Configuration.GetValue<bool>("Security:HttpsTerminatedByProxy"))
    app.UseHttpsRedirection();
app.UseBrowserRequestSecurity();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapProfileMediaEndpoints();
app.MapProfiles();
app.MapAuthEndpoints();
app.MapMatchEndpoints();
app.MapDuelEndpoints();
app.MapRunCodeEndpoints();
app.MapSubmissionEndpoints();
app.MapProblemEndpoints();
app.MapPracticeEndpoints();

app.Run();

