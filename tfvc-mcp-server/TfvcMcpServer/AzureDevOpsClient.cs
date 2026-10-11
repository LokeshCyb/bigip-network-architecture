using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TfvcMcpServer;

public sealed class AzureDevOpsClient
{
    private const string ApiVersion = "7.1";
    private const int MaxErrorChars = 500;
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AzureDevOpsClient(IOptions<AzureDevOpsOptions> options, HttpClient http)
    {
        var settings = options.Value;
        Pat = settings.Pat;
        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(settings.Organization)}/{Uri.EscapeDataString(settings.Project)}/_apis";
        _http = http;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + Pat)));
    }

    private string Pat { get; }

    public Task<string> QueryWorkItemsAsync(string wiql, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"wit/wiql?api-version={ApiVersion}",
            new StringContent(JsonSerializer.Serialize(new { query = wiql }), Encoding.UTF8, "application/json"), ct);

    public Task<string> GetWorkItemAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"wit/workitems/{id}?$expand=all&api-version={ApiVersion}", null, ct);

    public Task<string> ListWorkItemTypesAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"wit/workitemtypes?api-version={ApiVersion}", null, ct);

    public Task<string> CreateWorkItemAsync(string type, string title, string? description, CancellationToken ct)
    {
        var patch = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title }
        };
        if (!string.IsNullOrWhiteSpace(description))
            patch.Add(new { op = "add", path = "/fields/System.Description", value = description });

        var content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json-patch+json");
        return SendAsync(HttpMethod.Post, $"wit/workitems/${Uri.EscapeDataString(type)}?api-version={ApiVersion}", content, ct);
    }

    public async Task<string> UpdateWorkItemAsync(int id, string patchJson, CancellationToken ct)
    {
        JsonDocument patch;
        try
        {
            patch = JsonDocument.Parse(patchJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Work item updates must be valid JSON Patch.", nameof(patchJson), ex);
        }

        using (patch)
        {
            if (patch.RootElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Work item updates must be a JSON Patch array.", nameof(patchJson));

            var content = new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json");
            return await SendAsync(HttpMethod.Patch, $"wit/workitems/{id}?api-version={ApiVersion}", content, ct);
        }
    }

    public Task<string> ListRepositoriesAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories?api-version={ApiVersion}", null, ct);

    public Task<string> GetRepositoryAsync(string repositoryId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories/{Uri.EscapeDataString(repositoryId)}?api-version={ApiVersion}", null, ct);

    private async Task<string> SendAsync(HttpMethod method, string relative, HttpContent? content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Pat) || Pat.StartsWith("<", StringComparison.Ordinal))
            throw new InvalidOperationException("AzureDevOps:Pat is not configured. Set it in appsettings.Local.json, user-secrets or the AzureDevOps__Pat environment variable.");

        using var request = new HttpRequestMessage(method, $"{_baseUrl}/{relative}") { Content = content };
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure DevOps returned {(int)response.StatusCode}: {Truncate(body, MaxErrorChars)}");
        return body;
    }

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "...[truncated]";
}
