using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TfvcMcpServer;

[McpServerToolType]
public sealed class AzureDevOpsTools(AzureDevOpsClient client)
{
    [McpServerTool(Name = "query_work_items"), Description("Run a WIQL query for Azure Boards work items such as user stories, tasks, and bugs.")]
    public Task<string> QueryWorkItems([Description("WIQL query text")] string wiql, CancellationToken ct = default) =>
        client.QueryWorkItemsAsync(wiql, ct);

    [McpServerTool(Name = "get_work_item"), Description("Get an Azure Boards work item by ID.")]
    public Task<string> GetWorkItem(int id, CancellationToken ct = default) =>
        client.GetWorkItemAsync(id, ct);

    [McpServerTool(Name = "list_work_item_types"), Description("List work item types available in the configured Azure DevOps project.")]
    public Task<string> ListWorkItemTypes(CancellationToken ct = default) =>
        client.ListWorkItemTypesAsync(ct);

    [McpServerTool(Name = "create_work_item"), Description("Create an Azure Boards work item, such as a user story, task, or bug.")]
    public Task<string> CreateWorkItem(string type, string title, string? description = null, CancellationToken ct = default) =>
        client.CreateWorkItemAsync(type, title, description, ct);

    [McpServerTool(Name = "update_work_item"), Description("Update an Azure Boards work item using a JSON Patch array.")]
    public Task<string> UpdateWorkItem(int id, [Description("JSON Patch operations")] string patchJson, CancellationToken ct = default) =>
        client.UpdateWorkItemAsync(id, patchJson, ct);

    [McpServerTool(Name = "list_repositories"), Description("List Git repositories in the configured Azure DevOps project.")]
    public Task<string> ListRepositories(CancellationToken ct = default) =>
        client.ListRepositoriesAsync(ct);

    [McpServerTool(Name = "get_repository"), Description("Get an Azure DevOps Git repository by ID or name.")]
    public Task<string> GetRepository(string repositoryId, CancellationToken ct = default) =>
        client.GetRepositoryAsync(repositoryId, ct);
}
