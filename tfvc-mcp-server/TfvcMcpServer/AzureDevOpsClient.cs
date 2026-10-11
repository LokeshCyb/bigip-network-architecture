using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TfvcMcpServer;

public sealed class AzureDevOpsClient
{
    private const string ApiVersion = "7.1";
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AzureDevOpsClient(IOptions<AzureDevOpsOptions> options, HttpClient http)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Pat) || settings.Pat.StartsWith("<"))
            throw new InvalidOperationException("AzureDevOps:Pat is not configured. Set it in appsettings.Local.json, user-secrets or the AzureDevOps__Pat environment variable.");

        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(settings.Organization)}/{Uri.EscapeDataString(settings.Project)}/_apis";
        _http = http;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + settings.Pat)));
    }

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

    public Task<string> UpdateWorkItemAsync(int id, string patchJson, CancellationToken ct)
    {
        using var patch = JsonDocument.Parse(patchJson);
        if (patch.RootElement.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Work item updates must be a JSON Patch array.", nameof(patchJson));

        var content = new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json");
        return SendAsync(HttpMethod.Patch, $"wit/workitems/{id}?api-version={ApiVersion}", content, ct);
    }

    public Task<string> ListRepositoriesAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories?api-version={ApiVersion}", null, ct);

    public Task<string> GetRepositoryAsync(string repositoryId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories/{Uri.EscapeDataString(repositoryId)}?api-version={ApiVersion}", null, ct);

    private async Task<string> SendAsync(HttpMethod method, string relative, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, $"{_baseUrl}/{relative}") { Content = content };
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure DevOps returned {(int)response.StatusCode}: {body[..Math.Min(body.Length, 500)]}");
        return body;
    }
}
