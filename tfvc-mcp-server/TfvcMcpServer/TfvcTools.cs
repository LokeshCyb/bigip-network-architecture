using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TfvcMcpServer;

[McpServerToolType]
public sealed class TfvcTools(TfvcClient client)
{
    [McpServerTool(Name = "list_items"), Description("List files/folders under a TFVC path (one level). Defaults to the configured root.")]
    public Task<string> ListItems([Description("TFVC path or path relative to root")] string? path = null, CancellationToken ct = default)
        => client.ListItemsAsync(client.Resolve(path), false, ct);

    [McpServerTool(Name = "get_file_content"), Description("Read the text content of a file (truncated at 200k chars).")]
    public Task<string> GetFileContent([Description("TFVC file path or path relative to root")] string path, CancellationToken ct = default)
        => client.GetFileAsync(client.Resolve(path), ct);

    [McpServerTool(Name = "search_files"), Description("Find files under a path whose name contains the pattern (e.g. '.csproj' or 'Controller').")]
    public async Task<string> SearchFiles(string pattern, [Description("Folder to search; defaults to root")] string? path = null, CancellationToken ct = default)
    {
        var json = await client.ListItemsAsync(client.Resolve(path), true, ct);
        using var doc = JsonDocument.Parse(json);
        var matches = doc.RootElement.GetProperty("value").EnumerateArray()
            .Where(i => !(i.TryGetProperty("isFolder", out var f) && f.GetBoolean()))
            .Select(i => i.GetProperty("path").GetString()!)
            .Where(p => p.Contains(pattern, StringComparison.OrdinalIgnoreCase) &&
                        p[(p.LastIndexOf('/') + 1)..].Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .Take(200).ToList();
        return JsonSerializer.Serialize(matches);
    }

    [McpServerTool(Name = "get_changesets"), Description("List recent changesets touching the root path.")]
    public Task<string> GetChangesets(int top = 20, CancellationToken ct = default) => client.GetChangesetsAsync(top, ct);

    [McpServerTool(Name = "get_changeset"), Description("Get changeset details.")]
    public Task<string> GetChangeset(int id, CancellationToken ct = default) => client.GetChangesetAsync(id, ct);

    [McpServerTool(Name = "get_changeset_changes"), Description("List files changed in a changeset.")]
    public Task<string> GetChangesetChanges(int id, CancellationToken ct = default) => client.GetChangesetChangesAsync(id, ct);
}
