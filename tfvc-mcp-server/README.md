# TFVC MCP Server

Read-only MCP server (stdio) for an Azure DevOps TFVC repository.

Environment variables:
- `AZDO_PAT` (required; PAT with Code (Read)) - never commit it
- `AZDO_ORG` (default `MG-Group-Holidays`), `AZDO_PROJECT` (default `MG-Grp`)
- `AZDO_ROOT_PATH` (default `$/MG-Grp/Source/Main-Dotnet-Upgrade`)

Run: `cd TfvcMcpServer && dotnet run`
Test: `npx @modelcontextprotocol/inspector dotnet run`

VS Code `.vscode/mcp.json`:
```json
{ "servers": { "tfvc": { "type": "stdio", "command": "dotnet",
  "args": ["run","--project","tfvc-mcp-server/TfvcMcpServer"],
  "env": { "AZDO_PAT": "${input:pat}" } } },
  "inputs": [{ "id": "pat", "type": "promptString", "password": true, "description": "Azure DevOps PAT" }] }
```
Tools: list_items, get_file_content, search_files, get_changesets, get_changeset, get_changeset_changes.
