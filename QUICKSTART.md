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

Alternatively, you can install the published NuGet tool directly (any OS):

```bash
dotnet tool install --global UnityMCP.Server
```

*Note: Installing as a global tool allows you to run `unity-mcp` from any directory.*

## 3. Verify Robustness (Run Tests)

Before connecting, ensure the environment is stable by running the comprehensive test suite:

```bash
dotnet test
```

*All 117 tests should pass, verifying protocol compliance, tool behaviour, and service mocking.*

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

*Note: If you didn't install it as a global tool, replace `unity-mcp` with `dotnet` and use `["run", "--project", "ABSOLUTE/PATH/TO/Unity-MCP-Server/UnityMCP.Server/UnityMCP.Server.csproj"]` in the args.*

## 5. Try it out

Use **projectPath** (project root), **fileName** (file name or path under project), and **folderName** (for folders/list). No duplicate path segments. Example calls:

- Create script: `unity_create_script(projectPath, "Assets/Scripts/SpaceshipController.cs", "SpaceshipController", content)` or `unity_save_script(projectPath, "SpaceshipController.cs", content)` (bare fileName goes under Assets/Scripts).
- Create scene: `unity_create_scene(projectPath, "Assets/Scenes/Level1.unity")`.
- List assets: `unity_list_assets(projectPath, "Assets", "*")`.

The server will automatically create any missing subfolders **under** your project root (for example `Assets/Scripts`, `Assets/Scenes`, `Assets/Audio`, `Assets/Text`, `Packages`) when writing files. The **projectPath** itself should already exist or be created by the scaffold tool.

Ask Claude:

> "Create a new script called 'SpaceshipController' in the Assets/Scripts folder of my Unity project."
>
> "Create a new Scene called 'Level1'."
>
> "List all assets in the project."
