using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Profile;
using Microsoft.AspNetCore.Http.Features;

namespace Api.Endpoints;

public static class ProfileMediaEndpoints
{
    private const int MaxRequestSize = 7 * 1024 * 1024;
    public const string UploadPolicy = "profile-media-upload";

    public static void MapProfileMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile/media").RequireAuthorization();
        group.MapGet("/{kind}", (string kind, ClaimsPrincipal user, HttpResponse response,
            ProfileMediaStorage storage, CancellationToken ct) =>
        {
            return OutputDimension(kind) == 0
                ? Task.FromResult(Results.NotFound())
                : ReadAsync(user.FindFirstValue(ClaimTypes.NameIdentifier)!, kind, response, storage, ct);
        });
        group.MapPut("/{kind}", UploadAsync).RequireRateLimiting(UploadPolicy);
    }

    private static async Task<IResult> UploadAsync(string kind, ClaimsPrincipal user, HttpRequest request,
        ProfileMediaStorage storage, CancellationToken ct)
    {
        var dimension = OutputDimension(kind);
        if (dimension == 0)
            return Results.NotFound();
        if (!storage.IsConfigured)
            return Results.StatusCode(503);

        if (request.ContentLength > MaxRequestSize)
            return Results.StatusCode(413);
        if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            limit.MaxRequestBodySize = MaxRequestSize;

        Upload? upload;
        try
        {
            upload = await request.ReadFromJsonAsync<Upload>(ct);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        if (upload?.Image is not { Length: > 0 } bytes)
            return Results.BadRequest();
        if (bytes.Length > ProfileImage.MaxFileSize)
            return Results.StatusCode(413);

        var normalized = ProfileImage.Normalize(bytes, dimension);
        if (normalized is null)
            return Results.Problem(statusCode: 422,
                detail: "Выберите статичное изображение JPEG, PNG или WebP до 4096 пикселей по каждой стороне и до 12 мегапикселей.");

        return await storage.WriteAsync(ObjectKey(user.FindFirstValue(ClaimTypes.NameIdentifier)!, kind), normalized, ct);
    }

    private static int OutputDimension(string kind) => kind switch
    {
        "avatar" => 512,
        "background" => 1920,
        _ => 0
    };

    internal static Task<IResult> ReadAsync(string userId, string kind, HttpResponse response,
        ProfileMediaStorage storage, CancellationToken ct)
    {
        response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; sandbox";
        return storage.ReadAsync(ObjectKey(userId, kind), ct);
    }

    private static string ObjectKey(string id, string kind)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(id)));
        return $"profiles/{hash}/{kind}.webp";
    }

    private sealed record Upload(byte[]? Image);
}
