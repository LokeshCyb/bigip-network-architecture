using Microsoft.AspNetCore.Http;
using TfvcMcpServer;

namespace TfvcMcpServer.Tests;

public class ApiKeyMiddlewareTests
{
    private static async Task<(int Status, bool Called)> Run(string path, Action<HttpRequest>? setup, params string[] keys)
    {
        var called = false;
        var mw = new ApiKeyMiddleware(_ => { called = true; return Task.CompletedTask; }, keys);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        setup?.Invoke(ctx.Request);
        await mw.InvokeAsync(ctx);
        return (ctx.Response.StatusCode, called);
    }

    [Fact]
    public async Task MissingKey_Returns401()
    {
        var (status, called) = await Run("/mcp", null, "secret");
        Assert.Equal(401, status);
        Assert.False(called);
    }

    [Fact]
    public async Task WrongKey_Returns401()
    {
        var (status, called) = await Run("/mcp", r => r.Headers["X-Api-Key"] = "nope", "secret");
        Assert.Equal(401, status);
        Assert.False(called);
    }

    [Fact]
    public async Task XApiKeyHeader_IsAccepted()
    {
        var (status, called) = await Run("/mcp", r => r.Headers["X-Api-Key"] = "secret", "other", "secret");
        Assert.Equal(200, status);
        Assert.True(called);
    }

    [Fact]
    public async Task BearerToken_IsAccepted()
    {
        var (_, called) = await Run("/mcp", r => r.Headers.Authorization = "Bearer " + "secret", "secret");
        Assert.True(called);
    }

    [Fact]
    public async Task Health_DoesNotRequireKey()
    {
        var (_, called) = await Run("/health", null, "secret");
        Assert.True(called);
    }

    [Fact]
    public void ParseKeys_SplitsCommasAndIgnoresPlaceholders()
    {
        var keys = ApiKeyMiddleware.ParseKeys(new string?[] { "a, b", "<placeholder>", null, "a" });
        Assert.Equal(new[] { "a", "b" }, keys);
    }
}
