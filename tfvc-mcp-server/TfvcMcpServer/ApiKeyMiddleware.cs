using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace TfvcMcpServer;

/// Requires a shared API key (X-Api-Key header or Authorization: Bearer) on every request except /health.
public sealed class ApiKeyMiddleware(RequestDelegate next, IReadOnlyCollection<string> apiKeys)
{
    private readonly byte[][] _keyHashes = apiKeys.Select(Hash).ToArray();

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase) || IsAuthorized(context.Request))
        {
            await next(context);
            return;
        }
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsync("Missing or invalid API key.");
    }

    private bool IsAuthorized(HttpRequest request)
    {
        var supplied = request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(supplied))
        {
            var auth = request.Headers.Authorization.FirstOrDefault();
            if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                supplied = auth[7..].Trim();
        }
        if (string.IsNullOrEmpty(supplied)) return false;
        var hash = Hash(supplied);
        var ok = false;
        foreach (var key in _keyHashes)
            ok |= CryptographicOperations.FixedTimeEquals(key, hash);
        return ok;
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    public static string[] ParseKeys(IEnumerable<string?> raw) =>
        raw.Where(k => !string.IsNullOrWhiteSpace(k))
           .SelectMany(k => k!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
           .Where(k => !(k.StartsWith('<') && k.EndsWith('>')))
           .Distinct().ToArray();
}
