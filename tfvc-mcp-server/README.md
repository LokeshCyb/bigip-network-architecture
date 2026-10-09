# TFVC MCP Server

Read-only MCP server (stdio) for an Azure DevOps TFVC repository.

Configuration (`TfvcMcpServer/appsettings.json`, section `AzureDevOps`): `Organization`, `Project`, `RootPath`, `Pat`.
Do not commit the PAT: copy `appsettings.Local.json.example` to `appsettings.Local.json` (git-ignored) and set `Pat`.
Environment variables override files, e.g. `AzureDevOps__Pat`.

Run: `cd TfvcMcpServer && dotnet run`
Unit tests: `dotnet test TfvcMcpServer.Tests`
Test: `npx @modelcontextprotocol/inspector dotnet run`

VS Code `.vscode/mcp.json`:
```json
{ "servers": { "tfvc": { "type": "stdio", "command": "dotnet",
  "args": ["run","--project","tfvc-mcp-server/TfvcMcpServer"],
  "env": {} } } }
```
Tools: list_items, get_file_content, search_files, get_changesets, get_changeset, get_changeset_changes.
