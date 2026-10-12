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
    public Task<string> GetWorkItem([Description("Work item ID")] int id, CancellationToken ct = default) =>
        client.GetWorkItemAsync(id, ct);

    [McpServerTool(Name = "list_work_item_types"), Description("List work item types available in the configured Azure DevOps project.")]
    public Task<string> ListWorkItemTypes(CancellationToken ct = default) =>
        client.ListWorkItemTypesAsync(ct);

    [McpServerTool(Name = "create_work_item"), Description("Create an Azure Boards work item, such as a user story, task, or bug.")]
    public Task<string> CreateWorkItem(
        [Description("Work item type, such as User Story, Task, or Bug")] string type,
        [Description("Work item title")] string title,
        [Description("Optional work item description")] string? description = null,
        CancellationToken ct = default) =>
        client.CreateWorkItemAsync(type, title, description, ct);

    [McpServerTool(Name = "update_work_item"), Description("Update an Azure Boards work item using a JSON Patch array.")]
    public Task<string> UpdateWorkItem(
        [Description("Work item ID")] int id,
        [Description("JSON Patch operations")] string patchJson,
        CancellationToken ct = default) =>
        client.UpdateWorkItemAsync(id, patchJson, ct);

    [McpServerTool(Name = "list_repositories"), Description("List Git repositories in the configured Azure DevOps project.")]
    public Task<string> ListRepositories(CancellationToken ct = default) =>
        client.ListRepositoriesAsync(ct);

    [McpServerTool(Name = "get_repository"), Description("Get an Azure DevOps Git repository by ID or name.")]
    public Task<string> GetRepository([Description("Git repository ID or name")] string repositoryId, CancellationToken ct = default) =>
        client.GetRepositoryAsync(repositoryId, ct);

    [McpServerTool(Name = "list_branches"), Description("List branches of a Git repository.")]
    public Task<string> ListBranches([Description("Git repository ID or name")] string repositoryId, CancellationToken ct = default) =>
        client.ListBranchesAsync(repositoryId, ct);

    [McpServerTool(Name = "list_pipelines"), Description("List pipelines in the configured Azure DevOps project.")]
    public Task<string> ListPipelines(CancellationToken ct = default) =>
        client.ListPipelinesAsync(ct);

    [McpServerTool(Name = "get_pipeline"), Description("Get a pipeline by ID.")]
    public Task<string> GetPipeline([Description("Pipeline ID")] int pipelineId, CancellationToken ct = default) =>
        client.GetPipelineAsync(pipelineId, ct);

    [McpServerTool(Name = "list_pipeline_runs"), Description("List runs of a pipeline.")]
    public Task<string> ListPipelineRuns([Description("Pipeline ID")] int pipelineId, CancellationToken ct = default) =>
        client.ListPipelineRunsAsync(pipelineId, ct);

    [McpServerTool(Name = "get_pipeline_run"), Description("Get a single pipeline run.")]
    public Task<string> GetPipelineRun([Description("Pipeline ID")] int pipelineId, [Description("Run ID")] int runId, CancellationToken ct = default) =>
        client.GetPipelineRunAsync(pipelineId, runId, ct);

    [McpServerTool(Name = "list_builds"), Description("List recent builds (classic and YAML), newest first.")]
    public Task<string> ListBuilds([Description("Max builds, 1-100")] int top = 20, CancellationToken ct = default) =>
        client.ListBuildsAsync(top, ct);

    [McpServerTool(Name = "get_build"), Description("Get build details by ID.")]
    public Task<string> GetBuild([Description("Build ID")] int buildId, CancellationToken ct = default) =>
        client.GetBuildAsync(buildId, ct);
}
