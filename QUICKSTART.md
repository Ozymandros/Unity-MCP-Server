# Quick Start Guide

## 1. Install .NET 10.0

Ensure you have the latest .NET 10 SDK installed.

```bash
dotnet --version
```

*Expect `10.0.x`*

## 2. Setup the Server

```bash
# Windows
./setup.ps1

# Linux/Mac
./setup.sh
```

*Note: This will build the solution and restore dependencies.*

## 3. Verify Robustness (Run Tests)

Before connecting, ensure the environment is stable by running the comprehensive test suite:

```bash
dotnet test
```

*All 9 tests should pass, verifying strict protocol compliance and service mocking.*

## 4. Connect to Claude Desktop

1. Open `%APPDATA%\Claude\claude_desktop_config.json` (Windows) or `~/Library/Application Support/Claude/claude_desktop_config.json` (Mac).
2. Add the configuration (replace `ABSOLUTE/PATH/TO/...` with your actual path):

   ```json
   {
     "mcpServers": {
       "unity": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "ABSOLUTE/PATH/TO/Unity-MCP-Server/UnityMcp.Server/UnityMcp.Server.csproj"
         ]
       }
     }
   }
   ```

3. Restart Claude Desktop.

## 5. Try it out

Ask Claude:

> "Create a new script called 'SpaceshipController' in the Assets/Scripts folder of my Unity project."
>
> "Create a new Scene called 'Level1'."
>
> "List all assets in the project."
