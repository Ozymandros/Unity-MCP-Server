# Unity MCP Server - Installation Guide

## Prerequisites

- Unity Editor 2020.3 or later
- .NET Standard 2.0 compatible
- System.Text.Json support (Unity 2020.3+)

## Step-by-Step Installation

### 1. Create Directory Structure

In your Unity project, create the following folder structure:

```
YourUnityProject/
└── Assets/
    └── UnityMCP/
        └── Editor/
            ├── Core/
            └── Tools/
```

### 2. Copy Core Files

Copy the following files to `Assets/UnityMCP/Editor/Core/`:

- `McpServer.cs`
- `McpMessage.cs`
- `McpDispatcher.cs`
- `McpToolRegistry.cs`

### 3. Copy Tool Files

Copy the following files to `Assets/UnityMCP/Editor/Tools/`:

- `PingTool.cs`
- `CreateSceneTool.cs`
- `CreateGameObjectTool.cs`
- `GetSceneInfoTool.cs`
- `CreateScriptTool.cs`

### 4. Verify Installation

1. Open your Unity project
2. Wait for scripts to compile
3. Check the Unity Console for startup messages:

Expected output:
```
[McpToolRegistry] Registered tool: ping
[McpToolRegistry] Registered tool: create_scene
[McpToolRegistry] Registered tool: create_gameobject
[McpToolRegistry] Registered tool: get_scene_info
[McpToolRegistry] Registered tool: create_script
[McpToolRegistry] Registered 5 tools
[McpDispatcher] Initialized
[McpServer] Unity MCP Server v1.0.0 started on port 8765
[McpServer] Registered 5 tools
```

### 5. Test Connection

Use the test client to verify the server is running:

#### Option A: Python Test
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 8765))

request = {
    "jsonrpc": "2.0",
    "id": "1",
    "method": "ping",
    "params": {}
}

sock.sendall((json.dumps(request) + "\n").encode('utf-8'))
response = sock.recv(8192).decode('utf-8')
print(response)
sock.close()
```

Expected output:
```json
{"jsonrpc":"2.0","id":"1","result":{"message":"pong","timestamp":"2025-02-14T..."}}
```

#### Option B: Telnet Test
```bash
telnet localhost 8765
```

Then type:
```json
{"jsonrpc":"2.0","id":"1","method":"ping","params":{}}
```

Press Enter.

## Configuration Options

### Changing the Server Port

Edit `Assets/UnityMCP/Editor/Core/McpServer.cs`:

```csharp
private const int DEFAULT_PORT = 8765; // Change to your desired port
```

After changing, restart Unity Editor.

### Adjusting Buffer Size

Edit `Assets/UnityMCP/Editor/Core/McpServer.cs`:

```csharp
byte[] buffer = new byte[8192]; // Change to your desired size
```

Larger buffers support bigger messages but use more memory.

### Disabling Auto-Start

If you want to manually start the server:

1. Comment out the auto-start in `McpServer.cs`:
```csharp
static McpServer()
{
    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    EditorApplication.quitting += OnEditorQuitting;
    // StartServer(); // Comment this line
}
```

2. Start server manually via Unity menu or script:
```csharp
McpServer.StartServer();
```

## Troubleshooting

### Issue: "Server already running"

**Cause**: Unity recompiled scripts while server was running

**Solution**: Restart Unity Editor

### Issue: "Port already in use"

**Cause**: Another application is using port 8765

**Solution**: 
- Close the other application, or
- Change the server port in configuration

### Issue: "No tools registered"

**Cause**: Tool files are not in the correct location

**Solution**:
- Verify all tool files are in `Assets/UnityMCP/Editor/Tools/`
- Check Unity Console for compilation errors
- Ensure all tools are in the `UnityMCP` namespace

### Issue: Connection timeout

**Cause**: Firewall blocking connections

**Solution**:
- Check Windows Firewall settings
- Allow Unity Editor through firewall
- Note: Server only accepts localhost connections by default

### Issue: Script compilation errors

**Cause**: Missing Unity version compatibility

**Solution**:
- Ensure Unity 2020.3 or later
- Check that System.Text.Json is available
- Update to latest Unity LTS version if needed

## Platform-Specific Notes

### Windows
- No additional configuration needed
- Server automatically starts when Unity loads

### macOS
- No additional configuration needed
- May need to allow Unity through firewall on first run

### Linux
- No additional configuration needed
- Ensure Unity has network permissions

## Network Security

By default, the server:
- Only listens on localhost (127.0.0.1)
- Does not accept remote connections
- Runs on port 8765

To enable remote connections (⚠️ NOT RECOMMENDED for production):

Edit `McpServer.cs`:
```csharp
_listener = new TcpListener(IPAddress.Any, port); // Accept from any IP
```

⚠️ **Warning**: This allows any machine on your network to control Unity Editor. Only use for development in trusted networks.

## Uninstallation

To remove the Unity MCP Server:

1. Close Unity Editor
2. Delete the `Assets/UnityMCP/` folder
3. Delete `Assets/UnityMCP.meta` file
4. Reopen Unity Editor

## Next Steps

After successful installation:

1. Read the [README.md](README.md) for usage examples
2. Try the example client scripts
3. Create your own custom tools
4. Integrate with your AI/LLM applications

## Getting Help

If you encounter issues:

1. Check Unity Console for error messages
2. Review this troubleshooting guide
3. Verify your Unity version compatibility
4. Check that all files are in correct locations

## Version Compatibility

| Unity Version | Status | Notes |
|---------------|--------|-------|
| 2020.3 LTS | ✅ Supported | Minimum version |
| 2021.3 LTS | ✅ Supported | Recommended |
| 2022.3 LTS | ✅ Supported | Fully tested |
| 2023.x | ✅ Supported | Latest features |
| 6000.x | ✅ Supported | Unity 6 |

## System Requirements

- **Operating System**: Windows 10/11, macOS 10.13+, Linux (Ubuntu 18.04+)
- **RAM**: 4GB minimum, 8GB recommended
- **Unity Editor**: 2020.3 or later
- **.NET**: Standard 2.0 compatible
