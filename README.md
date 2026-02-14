# Unity MCP Server

A complete Model Context Protocol (MCP) server implementation for Unity Editor that enables AI assistants and LLMs to interact with Unity projects programmatically.

## Overview

The Unity MCP Server provides a standardized JSON-RPC 2.0 interface for external applications to control and query Unity Editor. This enables powerful AI-assisted workflows for game development, scene creation, scripting, and more.

## Features

- **MCP Protocol Compliant**: Implements the Model Context Protocol specification (2025-11-25)
- **JSON-RPC 2.0**: Standard request/response messaging over TCP
- **Thread-Safe**: Ensures all Unity API calls execute on the main thread
- **Tool Registry**: Dynamic tool discovery and registration via reflection
- **Multiple Tools**: Pre-built tools for common Unity operations

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
│  └──────────┬───────────────────┘   │
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

## Installation

1. **Copy Files to Unity Project**:
   ```
   Assets/
   └── UnityMCP/
       └── Editor/
           ├── Core/
           │   ├── McpServer.cs
           │   ├── McpMessage.cs
           │   ├── McpDispatcher.cs
           │   └── McpToolRegistry.cs
           └── Tools/
               ├── PingTool.cs
               ├── CreateSceneTool.cs
               ├── CreateGameObjectTool.cs
               ├── GetSceneInfoTool.cs
               └── CreateScriptTool.cs
   ```

2. **Verify Installation**:
   - Open Unity Editor
   - Check Console for: `[McpServer] Unity MCP Server v1.0.0 started on port 8765`
   - Verify registered tools: `[McpToolRegistry] Registered 5 tools`

## Configuration

### Default Settings
- **Port**: 8765
- **Protocol**: JSON-RPC 2.0
- **Transport**: TCP (localhost only)
- **Buffer Size**: 8192 bytes

### Changing Port
Edit `McpServer.cs`:
```csharp
private const int DEFAULT_PORT = 8765; // Change to your desired port
```

## Available Tools

### 1. Ping Tool
**Method**: `ping`

Tests connectivity with the MCP server.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "ping",
  "params": {
    "message": "optional echo message"
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "message": "pong",
    "echo": "optional echo message",
    "timestamp": "2025-02-14T10:30:00.000Z"
  }
}
```

### 2. Create Scene Tool
**Method**: `create_scene`

Creates a new Unity scene.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "create_scene",
  "params": {
    "name": "MyNewScene",
    "path": "Assets/Scenes",
    "setup": "default"
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "success": true,
    "path": "Assets/Scenes/MyNewScene.unity",
    "name": "MyNewScene",
    "setup": "default",
    "objectCount": 2,
    "message": "Scene 'MyNewScene' created successfully"
  }
}
```

### 3. Create GameObject Tool
**Method**: `create_gameobject`

Creates GameObjects in the active scene.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "create_gameobject",
  "params": {
    "name": "MyCube",
    "type": "cube",
    "position": { "x": 0, "y": 1, "z": 0 }
  }
}
```

### 4. Get Scene Info Tool
**Method**: `get_scene_info`

Retrieves information about the current scene.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "method": "get_scene_info",
  "params": {
    "includeHierarchy": true,
    "includeComponents": false
  }
}
```

### 5. Create Script Tool
**Method**: `create_script`

Creates C# script files.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "method": "create_script",
  "params": {
    "name": "PlayerController",
    "type": "monobehaviour",
    "namespace": "Game.Controllers"
  }
}
```

## MCP Protocol Methods

### Initialize
**Method**: `initialize`

Required handshake when connecting.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "init",
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-11-25",
    "clientInfo": {
      "name": "My MCP Client",
      "version": "1.0.0"
    }
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
    "serverInfo": {
      "name": "Unity MCP Server",
      "version": "1.0.0"
    },
    "capabilities": {
      "tools": {}
    }
  }
}
```

### Tools List
**Method**: `tools/list`

Lists all available tools.

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
        "inputSchema": { /* JSON Schema */ }
      },
      // ... other tools
    ]
  }
}
```

### Tools Call (Standard MCP Format)
**Method**: `tools/call`

Executes a tool using standard MCP format.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "call",
  "method": "tools/call",
  "params": {
    "name": "create_scene",
    "arguments": {
      "name": "TestScene"
    }
  }
}
```

## Creating Custom Tools

### Basic Tool Structure

```csharp
using System.Text.Json;
using UnityEngine;

namespace UnityMCP
{
    public class MyCustomTool : BaseMcpTool
    {
        public override string GetName()
        {
            return "my_custom_tool";
        }

        public override string GetDescription()
        {
            return "Description of what this tool does";
        }

        public override object GetInputSchema()
        {
            return new
            {
                type = "object",
                properties = new
                {
                    param1 = new
                    {
                        type = "string",
                        description = "Description of param1"
                    }
                },
                required = new[] { "param1" }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            try
            {
                string param1 = GetStringParam(parameters, "param1");
                
                // Your tool logic here
                
                return new
                {
                    success = true,
                    result = "Your result data"
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MyCustomTool] Error: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }
    }
}
```

Tools are automatically discovered and registered via reflection when Unity loads.

## Client Examples

### Python Client

```python
import socket
import json

class UnityMCPClient:
    def __init__(self, host='localhost', port=8765):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect((host, port))
        self.request_id = 0
        
    def send_request(self, method, params=None):
        self.request_id += 1
        request = {
            "jsonrpc": "2.0",
            "id": str(self.request_id),
            "method": method,
            "params": params or {}
        }
        
        message = json.dumps(request) + "\n"
        self.sock.sendall(message.encode('utf-8'))
        
        response = self.sock.recv(8192).decode('utf-8')
        return json.loads(response.strip())
    
    def close(self):
        self.sock.close()

# Usage
client = UnityMCPClient()

# Initialize
response = client.send_request("initialize", {
    "protocolVersion": "2025-11-25",
    "clientInfo": {"name": "Python Client", "version": "1.0"}
})
print(response)

# List tools
response = client.send_request("tools/list")
print(response)

# Create scene
response = client.send_request("create_scene", {"name": "TestScene"})
print(response)

client.close()
```

### JavaScript/Node.js Client

```javascript
const net = require('net');

class UnityMCPClient {
    constructor(host = 'localhost', port = 8765) {
        this.client = new net.Socket();
        this.client.connect(port, host);
        this.requestId = 0;
    }

    sendRequest(method, params = {}) {
        return new Promise((resolve, reject) => {
            this.requestId++;
            const request = {
                jsonrpc: "2.0",
                id: String(this.requestId),
                method: method,
                params: params
            };

            this.client.once('data', (data) => {
                const response = JSON.parse(data.toString().trim());
                resolve(response);
            });

            this.client.once('error', reject);
            this.client.write(JSON.stringify(request) + '\n');
        });
    }

    close() {
        this.client.destroy();
    }
}

// Usage
(async () => {
    const client = new UnityMCPClient();
    
    // Initialize
    const initResponse = await client.sendRequest('initialize', {
        protocolVersion: '2025-11-25',
        clientInfo: { name: 'JS Client', version: '1.0' }
    });
    console.log(initResponse);
    
    // Create scene
    const sceneResponse = await client.sendRequest('create_scene', {
        name: 'TestScene'
    });
    console.log(sceneResponse);
    
    client.close();
})();
```

## Troubleshooting

### Server Not Starting
- Check Unity Console for error messages
- Ensure port 8765 is not in use
- Verify all scripts are in correct folders

### Connection Refused
- Ensure Unity Editor is running
- Check firewall settings
- Verify server is listening: Check for "started on port" message

### Tools Not Registered
- Check Unity Console for registration messages
- Ensure tools inherit from `IMcpTool` or `BaseMcpTool`
- Verify tools are in correct namespace

### Main Thread Errors
- All Unity API calls must go through tools
- Tools automatically execute on main thread
- Never call Unity APIs directly from async code

## Performance Considerations

- Each client connection runs in a separate task
- Main thread queue processes all Unity API calls
- Multiple clients can connect simultaneously
- Buffer size: 8192 bytes (configurable)

## Security Considerations

⚠️ **Important Security Notes**:
- Server only listens on localhost (127.0.0.1)
- No authentication mechanism (add if exposing remotely)
- Runs with full Unity Editor permissions
- Validate all input in custom tools
- Never expose to untrusted networks

## Contributing

To add new tools:
1. Create a new class implementing `IMcpTool` or extending `BaseMcpTool`
2. Place in `Assets/UnityMCP/Editor/Tools/`
3. Tool will auto-register on next Unity compile

## License

This implementation is provided as-is for educational and development purposes.

## Resources

- [Model Context Protocol Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [Unity Editor Scripting](https://docs.unity3d.com/Manual/ExtendingTheEditor.html)

## Version History

- **1.0.0** (2025-02-14)
  - Initial release
  - Core MCP server implementation
  - 5 built-in tools
  - Thread-safe main thread execution
  - Automatic tool discovery
