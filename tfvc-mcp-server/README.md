# Azure DevOps MCP Server

MCP server (stdio) for Azure DevOps TFVC, Boards work items, and Git repositories.

Open `AzureDevOpsMcp.sln` in Visual Studio 2022 to build the server and test project together. Configuration (`TfvcMcpServer/appsettings.json`, section `AzureDevOps`): `Organization`, `Project`, `RootPath`, `Pat`.
Do not commit the PAT: copy `appsettings.Local.json.example` to `appsettings.Local.json` (git-ignored) and set `Pat`.
Environment variables override files, e.g. `AzureDevOps__Pat`.

Run from this directory: `dotnet run --project TfvcMcpServer`
Unit tests: `dotnet test AzureDevOpsMcp.sln`
Test interactively: `npx @modelcontextprotocol/inspector dotnet run --project TfvcMcpServer`

VS Code `.vscode/mcp.json`:
```json
{ "servers": { "tfvc": { "type": "stdio", "command": "dotnet",
  "args": ["run","--project","tfvc-mcp-server/TfvcMcpServer"],
  "env": {} } } }
```

TFVC tools: `list_items`, `get_file_content`, `search_files`, `get_changesets`, `get_changeset`, `get_changeset_changes`.

Azure Boards tools: `query_work_items` (WIQL), `get_work_item`, `list_work_item_types`, `create_work_item`, `update_work_item`.
The type is supplied when creating a work item, so it supports project-defined types such as user stories, tasks, and bugs. Updates use a JSON Patch array.

Git tools: `list_repositories`, `get_repository`.
Use a PAT with the minimum required Azure DevOps Code and Work Items permissions. The server does not expose every Azure DevOps REST API; it provides these common work-item and repository operations alongside TFVC.
