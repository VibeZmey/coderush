using Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Api;

public sealed class BrowserRequestSecurityTests
{
    [Theory]
    [InlineData("https://localhost:8443", "same-origin", true)]
    [InlineData("https://evil.example", "cross-site", false)]
    [InlineData("https://evil.example", "same-origin", false)]
    [InlineData("https://localhost:8443.evil.example", "same-origin", false)]
    [InlineData("null", "same-origin", false)]
    [InlineData("https://localhost:8443/path", "same-origin", false)]
    [InlineData("https://localhost:8443", "cross-site", false)]
    [InlineData(null, null, true)]
    public void ValidatesBrowserOriginWithoutTrustingSuffixes(string? origin, string? fetchSite, bool expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Host = new HostString("localhost", 8443);
        if (origin is not null) context.Request.Headers.Origin = origin;
        if (fetchSite is not null) context.Request.Headers["Sec-Fetch-Site"] = fetchSite;
        Assert.Equal(expected, BrowserRequestSecurity.IsAllowed(context.Request));
    }

    [Theory]
    [InlineData("application/json", 200)]
    [InlineData("application/json; charset=utf-8", 200)]
    [InlineData("application/x-www-form-urlencoded", 415)]
    [InlineData("text/plain", 415)]
    [InlineData(null, 415)]
    public async Task RejectsSimpleFormSubmissionsBeforeReachingEndpoints(string? contentType, int expectedStatus)
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseBrowserRequestSecurity();
        app.Run(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";
        context.Request.Method = "POST";
        context.Request.ContentType = contentType;
        await app.Build()(context);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
    }
}
