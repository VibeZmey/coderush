namespace Api.Security;

/// <summary>
/// Protects cookie-authenticated JSON APIs from cross-origin browser writes without
/// adding token endpoints. Non-browser clients with no Origin remain supported.
/// </summary>
public static class BrowserRequestSecurity
{
    public static bool IsAllowed(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method))
            return true;

        if (request.Headers["Sec-Fetch-Site"] == "cross-site")
            return false;

        var origin = request.Headers.Origin;
        if (origin.Count == 0)
            return true;

        return origin.Count == 1
            && Uri.TryCreate(origin[0], UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || uri.Scheme == "http")
            && uri.PathAndQuery == "/"
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }

    public static IApplicationBuilder UseBrowserRequestSecurity(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.XContentTypeOptions = "nosniff";
                if (!IsAllowed(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                // JSON prevents simple cross-origin form submissions. Preserve
                // the existing bodyless logout contract for Swagger/CLI clients.
                var bodylessLogout = context.Request.Path == "/api/auth/logout"
                    && context.Request.ContentLength.GetValueOrDefault() == 0
                    && context.Request.Headers.TransferEncoding.Count == 0
                    && context.Request.ContentType is null;
                if (!HttpMethods.IsGet(context.Request.Method)
                    && !HttpMethods.IsHead(context.Request.Method)
                    && !HttpMethods.IsOptions(context.Request.Method)
                    && !context.Request.HasJsonContentType() && !bodylessLogout)
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }
            }
            await next(context);
        });
}
