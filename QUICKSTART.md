# Quick Start Guide

## 1. Install .NET 10.0

Ensure you have the latest .NET 10 SDK installed.

```bash
dotnet --version
```

*Expect `10.0.x`*

## 2. Setup the Server

```bash
# Windows (Standard Setup)
./setup.ps1

# Windows (Global Tool Installation - RECOMMENDED)
./install-tool.ps1

# Linux/Mac
./setup.sh
```

*Note: The global tool installation allows you to run `unity-mcp` from any directory.*

## 3. Verify Robustness (Run Tests)

Before connecting, ensure the environment is stable by running the comprehensive test suite:

```bash
dotnet test
```

*All 9 tests should pass, verifying strict protocol compliance and service mocking.*

## 4. Configuration Examples

### Claude Desktop
Open `%APPDATA%\Claude\claude_desktop_config.json` (Windows) or `~/Library/Application Support/Claude/claude_desktop_config.json` (Mac) and add:

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "unity-mcp",
      "args": [],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```


### Google Antigravity / GenAI Agents
In your `mcp_config.json`:

```json
{
  "mcpServers": {
    "unity-mcp-server": {
      "command": "unity-mcp",
      "args": [],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```


### Cursor / Windsurf / VS Code

For IDEs that support MCP extensions, you can add it through the UI or settings using the same logic:

- **Command**: `unity-mcp`
- **Args**: (keep empty)
- **Env Vars**: `DOTNET_ENVIRONMENT=Development`

*Note: If you didn't install it as a global tool, replace `unity-mcp` with `dotnet` and use `["run", "--project", "ABSOLUTE/PATH/TO/Unity-MCP-Server/UnityMcp.Server/UnityMcp.Server.csproj"]` in the args.*

## 5. Try it out

Ask Claude:

> "Create a new script called 'SpaceshipController' in the Assets/Scripts folder of my Unity project."
>
> "Create a new Scene called 'Level1'."
>
> "List all assets in the project."
