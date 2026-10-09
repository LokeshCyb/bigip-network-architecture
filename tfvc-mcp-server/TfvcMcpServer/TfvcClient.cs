using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace TfvcMcpServer;

public sealed class TfvcClient
{
    private const string ApiVersion = "7.1";
    private const int MaxFileChars = 200_000;
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    public string RootPath { get; }

    public TfvcClient(IOptions<AzureDevOpsOptions> options)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.Pat) || o.Pat.StartsWith("<"))
            throw new InvalidOperationException("AzureDevOps:Pat is not configured. Set it in appsettings.Local.json, user-secrets or the AzureDevOps__Pat environment variable.");
        RootPath = o.RootPath.Replace('\\', '/').TrimEnd('/');
        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(o.Organization)}/{Uri.EscapeDataString(o.Project)}/_apis/tfvc";
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + o.Pat)));
    }

    /// Resolves a path and ensures it stays inside the configured root.
    public string Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return RootPath;
        var p = path.Replace('\\', '/').Trim();
        if (!p.StartsWith("$/")) p = RootPath + "/" + p.TrimStart('/');
        if (p.Split('/').Any(s => s == "..")) throw new ArgumentException("Path traversal is not allowed.");
        p = p.TrimEnd('/');
        if (p != RootPath && !p.StartsWith(RootPath + "/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Path must be under {RootPath}.");
        return p;
    }

    private async Task<string> GetAsync(string relative, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"{_baseUrl}/{relative}", ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure DevOps returned {(int)resp.StatusCode}: {Truncate(body, 500)}");
        return body;
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "...[truncated]";

    public Task<string> ListItemsAsync(string path, bool recursive, CancellationToken ct) =>
        GetAsync($"items?scopePath={Uri.EscapeDataString(path)}&recursionLevel={(recursive ? "Full" : "OneLevel")}&api-version={ApiVersion}", ct);

    public async Task<string> GetFileAsync(string path, CancellationToken ct) =>
        Truncate(await GetAsync($"items?path={Uri.EscapeDataString(path)}&download=true&api-version={ApiVersion}", ct), MaxFileChars);

    public Task<string> GetChangesetsAsync(int top, CancellationToken ct) =>
        GetAsync($"changesets?searchCriteria.itemPath={Uri.EscapeDataString(RootPath)}&$top={Math.Clamp(top, 1, 100)}&api-version={ApiVersion}", ct);

    public Task<string> GetChangesetAsync(int id, CancellationToken ct) =>
        GetAsync($"changesets/{id}?api-version={ApiVersion}", ct);

    public Task<string> GetChangesetChangesAsync(int id, CancellationToken ct) =>
        GetAsync($"changesets/{id}/changes?api-version={ApiVersion}", ct);
}

public sealed class AzureDevOpsOptions
{
    public string Organization { get; set; } = "MG-Group-Holidays";
    public string Project { get; set; } = "MG-Grp";
    public string RootPath { get; set; } = "$/MG-Grp/Source/Main-Dotnet-Upgrade";
    public string Pat { get; set; } = "";
}
