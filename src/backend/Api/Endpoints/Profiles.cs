using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Api.Profile;
using Api.Validation;
using Domain.Models.Matches;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Endpoints;

public static class Profiles
{
    private const int PageSize = 20;

    public static void MapProfiles(this IEndpointRouteBuilder app)
    {
        var profiles = app.MapGroup("/api/profiles");
        profiles.MapGet("/{id:guid}", GetAsync);
        profiles.MapGet("/{id:guid}/matches", HistoryAsync);
        profiles.MapGet("/{id:guid}/media/{kind}", async (Guid id, string kind, HttpResponse response,
            AppDbContext db, ProfileMediaStorage storage, CancellationToken ct) =>
        {
            var userId = id.ToString();
            if (kind is not ("avatar" or "background")
                || !await db.Users.AnyAsync(x => x.Id == userId, ct))
                return Results.NotFound();

            return await ProfileMediaEndpoints.ReadAsync(userId, kind, response, storage, ct);
        });
        app.MapPut("/api/profile/settings", SaveAsync).RequireAuthorization()
            .AddEndpointFilter<RequestValidationFilter<SettingsRequest>>();
    }

    private static async Task<IResult> GetAsync(Guid id, AppDbContext db, DuelService duels, CancellationToken ct)
    {
        var userId = id.ToString();
        var adminIds = db.AdminUserIds();
        var profile = await db.Users.Where(x => x.Id == userId)
            .Select(x => new { x.Id, x.UserName, x.Bio, x.RegisteredAt, IsAdmin = adminIds.Contains(x.Id) }).SingleOrDefaultAsync(ct);
        if (profile is null)
            return Results.NotFound();

        var today = DateTime.UtcNow.Date;
        var since = new DateTimeOffset(today.AddDays(-729));
        var until = new DateTimeOffset(today.AddDays(1));
        var dates = await Games(db, userId).Where(x => x.PlayedAt >= since && x.PlayedAt < until)
            .Select(x => x.PlayedAt).ToListAsync(ct);
        var activity = dates.GroupBy(x => DateOnly.FromDateTime(x.UtcDateTime))
            .ToDictionary(x => x.Key.ToString("yyyy-MM-dd"), x => x.Count());
        return Results.Ok(new { profile.Id, profile.UserName, profile.Bio, profile.RegisteredAt, profile.IsAdmin,
            Today = DateOnly.FromDateTime(today), Activity = activity, Rating = await duels.RatingAsync(userId, ct),
            Practice = await Practice.StatsAsync(db, userId, ct) });
    }

    private static async Task<IResult> HistoryAsync(Guid id, int? page, bool? duelsOnly, AppDbContext db, CancellationToken ct)
    {
        var userId = id.ToString();
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
            return Results.NotFound();

        var index = Math.Clamp(page ?? 0, 0, int.MaxValue / PageSize);
        var games = Games(db, userId);
        if (duelsOnly == true)
            games = games.Where(x => x.Mode != "Training");
        var items = await games.OrderByDescending(x => x.PlayedAt).ThenByDescending(x => x.Id)
            .Skip(index * PageSize).Take(PageSize + 1).ToListAsync(ct);
        return Results.Ok(new
        {
            Items = items.Take(PageSize).Select(game => new
            {
                game.Id, game.PlayedAt, game.Mode, game.Result, game.Rating, game.Delta,
                Rank = game.Rating is { } rating ? DuelRules.GetRank(rating).Rank.ToString() : null
            }),
            HasMore = items.Count > PageSize
        });
    }

    private static IQueryable<ProfileGame> Games(AppDbContext db, string userId)
    {
        var training = db.TrainingMatches.Where(x => x.UserId == userId && x.Status == MatchStatus.Completed && x.StartedAt != null)
            .Select(x => new ProfileGame
            {
                Id = x.Id,
                PlayedAt = x.FinishedAt ?? x.StartedAt!.Value,
                Mode = "Training",
                Result = x.FinishReason == MatchFinishReason.Abandoned ? "Abandoned" : "Completed",
                Rating = null,
                Delta = null
            });
        var duels = db.Set<DuelMatch>()
            .Where(x => x.Status == MatchStatus.Completed && x.StartedAt != null
                && (x.UserId == userId || x.OpponentId == userId))
            .Select(x => new ProfileGame
            {
                Id = x.Id,
                PlayedAt = x.FinishedAt ?? x.StartedAt!.Value,
                Mode = x.Ranked ? "Ranked" : "Custom",
                Result = x.Outcome == DuelOutcome.Draw ? "Draw" : x.Outcome == null || x.Outcome == DuelOutcome.JudgeUnavailable
                    ? "Unavailable" : (x.Outcome == DuelOutcome.FirstPlayerWon) == (x.UserId == userId) ? "Win" : "Loss",
                Rating = x.Ranked && x.Outcome != null && x.Outcome != DuelOutcome.JudgeUnavailable
                    ? x.UserId == userId ? x.FirstRating + x.FirstDelta : x.SecondRating + x.SecondDelta : null,
                Delta = x.Ranked && x.Outcome != null && x.Outcome != DuelOutcome.JudgeUnavailable
                    ? x.UserId == userId ? x.FirstDelta : x.SecondDelta : null
            });
        return training.Concat(duels);
    }

    private static async Task<IResult> SaveAsync(SettingsRequest input, ClaimsPrincipal principal,
        UserManager<User> users, SignInManager<User> signIn, AppDbContext db, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var emailChanged = !string.Equals(input.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase);
        var passwordChanged = !string.IsNullOrEmpty(input.NewPassword);
        if ((emailChanged || passwordChanged) && (string.IsNullOrEmpty(input.CurrentPassword)
            || !(await signIn.CheckPasswordSignInAsync(user, input.CurrentPassword, lockoutOnFailure: true)).Succeeded))
            return Results.Problem(statusCode: 400, detail: "Проверь текущий пароль.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            user.Bio = input.Bio?.Trim();
            var userName = input.UserName.Trim();
            var result = user.UserName == userName
                ? await users.UpdateAsync(user)
                : await users.SetUserNameAsync(user, userName);
            if (!result.Succeeded)
                return IdentityError(result);

            if (emailChanged)
            {
                result = await users.SetEmailAsync(user, input.Email.Trim());
                if (!result.Succeeded)
                    return IdentityError(result);
            }

            if (passwordChanged)
            {
                result = await users.ChangePasswordAsync(user, input.CurrentPassword!, input.NewPassword!);
                if (!result.Succeeded)
                    return IdentityError(result);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation } conflict)
        {
            return Results.Conflict(new { message = conflict.ConstraintName == "EmailIndex"
                ? "Эта почта уже зарегистрирована." : "Это имя пользователя уже занято." });
        }

        await signIn.RefreshSignInAsync(user);
        return Results.Ok(new { user.Id, user.UserName, user.Email,
            IsAdmin = principal.IsInRole(Application.Common.Constants.UserRoles.Admin) });
    }

    private static IResult IdentityError(IdentityResult result) => Results.BadRequest(new { errors = result.Errors });

    private sealed class ProfileGame
    {
        public Guid Id { get; init; }
        public DateTimeOffset PlayedAt { get; init; }
        public required string Mode { get; init; }
        public required string Result { get; init; }
        public int? Rating { get; init; }
        public int? Delta { get; init; }
    }

    public sealed class SettingsRequest
    {
        [Required, StringLength(256)]
        public required string UserName { get; init; }
        [Required, EmailAddress, StringLength(256)]
        public required string Email { get; init; }
        [StringLength(500)]
        public string? Bio { get; init; }
        [StringLength(1024)]
        public string? CurrentPassword { get; init; }
        [StringLength(1024)]
        public string? NewPassword { get; init; }
    }
}
