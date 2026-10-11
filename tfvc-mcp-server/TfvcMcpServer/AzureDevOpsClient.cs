using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TfvcMcpServer;

public sealed class AzureDevOpsClient
{
    private const string ApiVersion = "7.1";
    private const int MaxErrorChars = 500;
    private const string PlaceholderPatPrefix = "<";
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AzureDevOpsClient(IOptions<AzureDevOpsOptions> options, HttpClient http)
    {
        var settings = options.Value;
        Pat = settings.Pat;
        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(settings.Organization)}/{Uri.EscapeDataString(settings.Project)}/_apis";
        _http = http;
    }

    private string Pat { get; }

    public Task<string> QueryWorkItemsAsync(string wiql, CancellationToken ct) =>
        SendAsync(HttpMethod.Post, $"wit/wiql?api-version={ApiVersion}", ct,
            () => new StringContent(JsonSerializer.Serialize(new { query = wiql }), Encoding.UTF8, "application/json"));

    public Task<string> GetWorkItemAsync(int id, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"wit/workitems/{id}?$expand=all&api-version={ApiVersion}", ct);

    public Task<string> ListWorkItemTypesAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"wit/workitemtypes?api-version={ApiVersion}", ct);

    public Task<string> CreateWorkItemAsync(string type, string title, string? description, CancellationToken ct)
    {
        var patch = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title }
        };
        if (!string.IsNullOrWhiteSpace(description))
            patch.Add(new { op = "add", path = "/fields/System.Description", value = description });

        var patchJson = JsonSerializer.Serialize(patch);
        // Azure DevOps requires the literal '$' before the escaped work item type.
        return SendAsync(HttpMethod.Post, $"wit/workitems/${Uri.EscapeDataString(type)}?api-version={ApiVersion}", ct,
            () => new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
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
            throw new ArgumentException("patchJson is not valid JSON.", nameof(patchJson), ex);
        }

        using (patch)
        {
            if (patch.RootElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Work item updates must be a JSON Patch array.", nameof(patchJson));

            return await SendAsync(HttpMethod.Patch, $"wit/workitems/{id}?api-version={ApiVersion}", ct,
                () => new StringContent(patchJson, Encoding.UTF8, "application/json-patch+json"));
        }
    }

    public Task<string> ListRepositoriesAsync(CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories?api-version={ApiVersion}", ct);

    public Task<string> GetRepositoryAsync(string repositoryId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"git/repositories/{Uri.EscapeDataString(repositoryId)}?api-version={ApiVersion}", ct);

    private async Task<string> SendAsync(HttpMethod method, string relative, CancellationToken ct, Func<HttpContent?>? contentFactory = null)
    {
        // appsettings.Local.json.example uses '<...>' to mark the PAT placeholder.
        if (string.IsNullOrWhiteSpace(Pat) || Pat.StartsWith(PlaceholderPatPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("AzureDevOps:Pat is not configured. Set it in appsettings.Local.json, user-secrets or the AzureDevOps__Pat environment variable.");

        using var request = new HttpRequestMessage(method, $"{_baseUrl}/{relative}")
        {
            Content = contentFactory?.Invoke()
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(":" + Pat)));
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure DevOps returned {(int)response.StatusCode}: {Truncate(body, MaxErrorChars)}");
        return body;
    }

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..maxChars] + "...[truncated]";
}
