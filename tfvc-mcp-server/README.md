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

## Running as a hosted HTTP server (Docker on Ubuntu)

The same binary supports two modes: **stdio** (default, launched by the MCP client, no hosting needed) and **HTTP** (`--http` or `Mcp__Transport=http`, endpoint `/mcp`, health check `/health`). In HTTP mode a shared Azure DevOps PAT is used for all users, and every request to `/mcp` must carry an API key (`X-Api-Key: <key>` or `Authorization: ****** Users need no Entra ID or PAT. The server refuses to start in HTTP mode without at least one key. Audit trails in Azure DevOps show the PAT owner, so use a service account with minimum scopes.

1. `cp TfvcMcpServer/appsettings.Local.json.example TfvcMcpServer/appsettings.Local.json` and set `AzureDevOps:Pat` and `Mcp:ApiKeys` (the file must be world-readable, e.g. `chmod 644`, since the container runs as a non-root user; use e.g. `openssl rand -hex 32`). Optionally set `Organization`, `Project`, `RootPath`.
2. `docker compose up -d --build` (or `docker build -t azure-devops-mcp .` then `docker run -d -p 8080:8080 -v $PWD/TfvcMcpServer/appsettings.Local.json:/app/appsettings.Local.json:ro azure-devops-mcp`).
   Alternatively pass `-e AzureDevOps__Pat=... -e Mcp__ApiKeys=key1,key2` instead of the file.
3. Put it behind HTTPS (nginx/Caddy reverse proxy or a VPN/private network); the API key is sent in a header. Check: `curl http://host:8080/health`.
4. Clients (VS Code `.vscode/mcp.json`):
```json
{ "servers": { "azure-devops": { "type": "http", "url": "https://your-host/mcp",
  "headers": { "X-Api-Key": "<key>" } } } }
```

Pipeline/build tools: `list_pipelines`, `get_pipeline`, `list_pipeline_runs`, `get_pipeline_run`, `list_builds`, `get_build`. Extra Git tool: `list_branches`. The PAT also needs Build (read) scope for these.
