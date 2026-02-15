# Unity MCP Server v1.2.0

[![.NET CI](https://img.shields.io/github/actions/workflow/status/ozymandros/unity-mcp-server/dotnet.yml?branch=main&label=.NET%20CI&logo=dotnet&logoColor=white&style=flat-square)](https://github.com/ozymandros/unity-mcp-server/actions/workflows/dotnet.yml)
[![Quality](https://img.shields.io/github/actions/workflow/status/ozymandros/unity-mcp-server/static_analysis.yml?branch=main&label=Quality&logo=github-actions&logoColor=white&style=flat-square)](https://github.com/ozymandros/unity-mcp-server/actions/workflows/static_analysis.yml)
[![Schema](https://img.shields.io/github/actions/workflow/status/ozymandros/unity-mcp-server/schema_validation.yml?branch=main&label=Schema&logo=json&logoColor=white&style=flat-square)](https://github.com/ozymandros/unity-mcp-server/actions/workflows/schema_validation.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-orange?style=flat-square&logo=json)](https://modelcontextprotocol.io)
[![UPM](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity&logoColor=white&style=flat-square)](https://docs.unity3d.com/Manual/index.html)
[![Version](https://img.shields.io/badge/UPM-1.2.0-blue?style=flat-square)](https://github.com/ozymandros/unity-mcp-server/releases)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square&logo=opensourceinitiative&logoColor=white)](https://opensource.org/licenses/MIT)

A complete Model Context Protocol (MCP) server for Unity Editor and .NET. Enables AI assistants and LLMs to interact with Unity projects or standalone .NET servers via JSON-RPC 2.0.

---

## Features
- **Skill-based architecture**: Easily add new tools/skills for automation and integration.
- **JSON-RPC over TCP**: Standard protocol for interoperability with clients (Node.js, Python, Semantic Kernel, etc).
- **Unity and .NET support**: Use as a Unity Editor extension or as a standalone .NET server.
- **Docker & DevContainer ready**: Run in containers for CI/CD or cloud workflows.
- **Comprehensive test suite**: Ensures reliability and correctness.

---

## Quick Start

Get up and running in minutes!

### Prerequisites
- Unity Editor 2020.3 or later (for Unity integration)
- .NET 10.0 SDK (for standalone/.NET usage)
- Python 3.7+ or Node.js 14+ (for client examples)

### Installation (Unity)
1. Copy the entire `UnityMCP` folder to your Unity project:
   ```
   YourUnityProject/Assets/UnityMCP/
   ```
2. Wait for Unity to compile scripts. Check Console for:
   ```
   [McpToolRegistry] Registered 5 tools
   [McpServer] Unity MCP Server v1.2.0 started on port 8765
   ```

### Installation (.NET/Standalone)
1. Clone this repo and build:
   ```sh
   dotnet build UnityMCP.sln
   ```
2. Run the server:
   ```sh
   dotnet run --project UnityMCP.Server
   ```

### Test Connection
- **Python:**
  ```python
  import socket, json
  sock = socket.socket(); sock.connect(('localhost', 8765))
  req = {"jsonrpc": "2.0", "id": "1", "method": "ping", "params": {}}
  sock.send((json.dumps(req) + "\n").encode()); print(sock.recv(1024).decode()); sock.close()
  ```
- **Command Line:**
  ```powershell
  echo '{"jsonrpc":"2.0","id":"1","method":"ping","params":{}}' | ncat localhost 8765
  ```

---

## VS Code Dev Container & Docker

You can develop and run the Unity MCP Server in a fully containerized environment using VS Code Dev Containers and Docker.

### Dev Container (VS Code)
- Open the project in VS Code
- If prompted, "Reopen in Container" (requires Docker)
- The `.devcontainer/devcontainer.json` configures the environment with .NET SDK, PowerShell, and C# extensions
- Port 8765 is forwarded for MCP connections

### Docker (Standalone)
- Build the Docker image:
  ```sh
  docker build -t unity-mcp-server .
  ```
- Run the server in a container:
  ```sh
  docker run -p 8765:8765 unity-mcp-server
  ```
- The server will be accessible on `localhost:8765` from your host

---

## Version History

- **1.2.0** (2026-02-15)
  - Major documentation and protocol improvements
  - Unified .NET/Unity install and quickstart
  - Expanded API and troubleshooting docs
  - Added VS Code DevContainer and Docker support
- **1.1.0** (2026-02-14)
  - License-free CI/CD pipeline
  - Standalone test suite and solution reorg
- **1.0.0** (2025-02-14)
  - Initial release with core MCP protocol and 5 tools

---

## Troubleshooting
- **Server already running:** Restart Unity Editor.
- **Port already in use:** Change port in config or close other app.
- **No tools registered:** Check file locations and namespaces.
- **Connection timeout:** Check firewall and allow Unity/port 8765.
- **Script errors:** Ensure Unity 2020.3+ and System.Text.Json support.

---

## CI/CD Workflows
- **.NET CI:** Validates core logic and runs tests on every push/PR.
- **Static Analysis:** Checks code formatting and style.
- **Schema Validation:** Ensures package.json and UPM compliance.
- **UPM Packaging:** Automates Unity Package Manager releases.

See [Documentation~/CI_CD.md](Documentation~/CI_CD.md) for details.

---

## Project Structure
- **UnityMCP.Server/**: Standalone .NET server entry point
- **StandaloneMCP/**: Core server logic, no Unity dependencies
- **UnityMCP.Core/**: Unity Editor integration (not required for standalone)
- **UnityMCP.Tests/**: Test suite for core and server logic
- **skills/**: MCP skill manifest and documentation
- **Documentation~/**: Architecture, API reference, CI/CD, installation, and quickstart docs

---

## Architecture

```
┌─────────────────┐
│  MCP Client     │
│  (AI/LLM App)   │
└────────┬────────┘
         │ TCP (JSON-RPC 2.0)
         │
┌────────▼────────────────────────────┐
│      Unity MCP Server               │
│  ┌──────────────────────────────┐   │
│  │     McpServer                │   │
│  │  (TCP Listener & Router)     │   │
│  └──────────┬─────────────────────────┘   │
│             │                        │
│  ┌──────────▼───────────────────┐   │
│  │     McpDispatcher            │   │
│  │  (Main Thread Executor)      │   │
│  └──────────┬───────────────────┘   │
│             │                        │
│  ┌──────────▼───────────────────┐   │
│  │    McpToolRegistry           │   │
│  │  (Tool Discovery & Routing)  │   │
│  └──────────┬───────────────────┘   │
│             │                        │
│  ┌──────────▼───────────────────┐   │
│  │       MCP Tools              │   │
│  │  • PingTool                  │   │
│  │  • CreateSceneTool           │   │
│  │  • CreateGameObjectTool      │   │
│  │  • GetSceneInfoTool          │   │
│  │  • CreateScriptTool          │   │
│  └──────────────────────────────┘   │
└─────────────────────────────────────┘
```

---

## Protocol & API Reference

> **See also:** [Documentation~/API_REFERENCE.md](Documentation~/API_REFERENCE.md) for the full, always-up-to-date protocol and tool documentation.

### MCP Protocol Methods

#### initialize
Establishes the MCP connection and exchanges capabilities.

**Method**: `initialize`

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "init",
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-11-25",
    "clientInfo": { "name": "Client Name", "version": "1.0.0" }
  }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "init",
  "result": {
    "protocolVersion": "2025-11-25",
    "serverInfo": { "name": "Unity MCP Server", "version": "1.2.0" },
    "capabilities": { "tools": {} }
  }
}
```

#### tools/list
Lists all registered tools with their schemas.

**Method**: `tools/list`

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "list",
  "method": "tools/list",
  "params": {}
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "list",
  "result": {
    "tools": [
      {
        "name": "ping",
        "description": "Simple ping test",
        "inputSchema": { "type": "object", "properties": { "message": { "type": "string", "description": "Optional message to echo" } } }
      }
    ]
  }
}
```

#### tools/call
Calls a tool using the standard MCP format.

**Method**: `tools/call`

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "call",
  "method": "tools/call",
  "params": {
    "name": "create_scene",
    "arguments": { "name": "MyScene" }
  }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "call",
  "result": {
    "content": [ { "type": "text", "text": "{\"success\":true,\"path\":\"Assets/Scenes/MyScene.unity\"}" } ]
  }
}
```

---

## Built-in Tools

### ping
Tests server connectivity and responsiveness.

**Method**: `ping`

**Parameters**:
- `message` (string, optional): Message to echo back

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "ping",
  "params": { "message": "Hello" }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": { "message": "pong", "echo": "Hello", "timestamp": "2025-02-14T10:30:00.000Z" }
}
```

### create_scene
Creates a new Unity scene file.

**Method**: `create_scene`

**Parameters**:
- `name` (string, required): Scene name
- `path` (string, optional): Path to save scene (default: "Assets/Scenes")
- `setup` (string, optional): Scene setup type - "default" or "empty" (default: "default")

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "create_scene",
  "params": { "name": "MainMenu", "path": "Assets/Scenes", "setup": "default" }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": { "success": true, "path": "Assets/Scenes/MainMenu.unity", "name": "MainMenu", "setup": "default", "objectCount": 2, "message": "Scene 'MainMenu' created successfully at Assets/Scenes/MainMenu.unity" }
}
```

### create_gameobject
Creates a GameObject in the active scene.

**Method**: `create_gameobject`

**Parameters**:
- `name` (string, required): GameObject name
- `type` (string, required): Object type - "empty", "cube", "sphere", "capsule", "cylinder", "plane", "quad"
- `position` (object, optional): World position {x, y, z}
- `parent` (string, optional): Parent GameObject name

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "create_gameobject",
  "params": { "name": "Player", "type": "cube", "position": { "x": 0, "y": 1, "z": 0 } }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "result": { "success": true, "name": "Player", "instanceId": 12345, "type": "cube", "position": { "x": 0.0, "y": 1.0, "z": 0.0 }, "message": "GameObject 'Player' created successfully" }
}
```

### get_scene_info
Retrieves detailed information about the active scene.

**Method**: `get_scene_info`

**Parameters**:
- `includeHierarchy` (boolean, optional): Include full GameObject hierarchy (default: true)
- `includeComponents` (boolean, optional): Include component information (default: false)

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "method": "get_scene_info",
  "params": { "includeHierarchy": true, "includeComponents": false }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "result": { "success": true, "name": "SampleScene", "path": "Assets/Scenes/SampleScene.unity", "isLoaded": true, "isDirty": false, "buildIndex": 0, "rootCount": 3, "totalObjectCount": 5, "rootObjects": [ { "name": "Main Camera", "tag": "MainCamera", "layer": "Default", "active": true, "static": false, "instanceId": 1234, "position": { "x": 0, "y": 1, "z": -10 } } ] }
}
```

### create_script
Creates a C# script file in the project.

**Method**: `create_script`

**Parameters**:
- `name` (string, required): Script class name
- `type` (string, optional): Script type - "monobehaviour", "scriptableobject", "plain", "interface" (default: "monobehaviour")
- `path` (string, optional): Save path (default: "Assets/Scripts")
- `namespace` (string, optional): Namespace for the script

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "method": "create_script",
  "params": { "name": "PlayerController", "type": "monobehaviour", "path": "Assets/Scripts/Player", "namespace": "Game.Controllers" }
}
```
**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "result": { "success": true, "path": "Assets/Scripts/Player/PlayerController.cs", "name": "PlayerController", "type": "monobehaviour", "message": "Script 'PlayerController' created successfully at Assets/Scripts/Player/PlayerController.cs" }
}
```

---

## Error Codes

| Code | Name | Description |
|------|------|-------------|
| -32700 | Parse error | Invalid JSON |
| -32600 | Invalid Request | Invalid request object |
| -32601 | Method not found | Method does not exist |
| -32602 | Invalid params | Invalid method parameters |
| -32603 | Internal error | Internal server error |

**Error Response Format**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "error": { "code": -32601, "message": "Method not found: unknown_method", "data": "Additional error information" }
}
```

---

## Usage Patterns & Best Practices

### Sequential Operations
Create a scene, add objects, and verify:
```python
client.send_request("create_scene", {"name": "Level1"})
client.send_request("create_gameobject", {"name": "Ground", "type": "plane", "position": {"x": 0, "y": 0, "z": 0}})
client.send_request("create_gameobject", {"name": "Player", "type": "cube", "position": {"x": 0, "y": 1, "z": 0}})
info = client.send_request("get_scene_info", {})
```

### Hierarchical Objects
```python
client.send_request("create_gameobject", {"name": "Enemy", "type": "sphere"})
client.send_request("create_gameobject", {"name": "WeaponSlot", "type": "empty", "parent": "Enemy"})
```

### Script Generation Workflow
```python
client.send_request("create_script", {"name": "EnemyAI", "type": "monobehaviour", "namespace": "Game.AI"})
# Wait for Unity to compile, then use the script
```

### Best Practices
- Always call `initialize` before using other methods
- Check `result.success` in responses
- Handle errors using the error codes above
- Close connections when done
- Validate parameters before sending
- Use appropriate types for each tool

---
