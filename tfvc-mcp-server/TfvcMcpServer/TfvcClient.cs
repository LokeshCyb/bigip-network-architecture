using System.Net.Http.Headers;
using System.Text;

namespace TfvcMcpServer;

public sealed class TfvcClient
{
    private const string ApiVersion = "7.1";
    private const int MaxFileChars = 200_000;
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    public string RootPath { get; }

    public TfvcClient()
    {
        var org = Env("AZDO_ORG", "MG-Group-Holidays");
        var project = Env("AZDO_PROJECT", "MG-Grp");
        RootPath = Env("AZDO_ROOT_PATH", "$/MG-Grp/Source/Main-Dotnet-Upgrade").TrimEnd('/');
        var pat = Environment.GetEnvironmentVariable("AZDO_PAT")
            ?? throw new InvalidOperationException("AZDO_PAT environment variable is required.");
        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(org)}/{Uri.EscapeDataString(project)}/_apis/tfvc";
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + pat)));
    }

    private static string Env(string name, string fallback)
    {
        var v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(v) ? fallback : v;
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
