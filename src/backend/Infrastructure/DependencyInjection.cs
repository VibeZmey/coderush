using Application.Common.Interfaces;
using Infrastructure.Identity;
using Application.Features.Problems.ManageProblems;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Application.Features.Code.RunCode;
using Infrastructure.Judge0;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Features.Code.SubmitCode;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdentityGateway, IdentityGateway>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ICodeRunRepository, CodeRunRepository>();
        services.AddScoped<ICodeRunReader, CodeRunRepository>();
        services.AddOptions<Judge0Options>().Bind(configuration.GetSection("Judge0"))
            .Validate(x => Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"), "Judge0:BaseUrl must be an HTTP URL.")
            .Validate(x => x.ProcessingTimeoutSeconds >= 30, "Judge0 timeout must be at least 30 seconds.")
            .ValidateOnStart();
        services.AddHttpClient<Judge0Client>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<Judge0Options>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(15);
            if (!string.IsNullOrWhiteSpace(settings.AuthToken)) client.DefaultRequestHeaders.Add("X-Auth-Token", settings.AuthToken);
        });
        services.AddScoped<CodeExecutionProcessor>();
        services.AddScoped<SubmissionProcessor>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddHostedService<CodeExecutionWorker>();
        services.AddScoped<DuelService>();
        services.AddHostedService<DuelWorker>();
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IProblemManagementRepository, ProblemManagementRepository>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CodeRush.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}

