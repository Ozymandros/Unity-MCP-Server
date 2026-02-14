# Unity MCP Server - Quick Start Guide

Get up and running with Unity MCP Server in 5 minutes!

## Prerequisites

✓ Unity Editor 2020.3 or later  
✓ Basic knowledge of JSON  
✓ Optional: Python 3.7+ or Node.js 14+ for examples

## Installation (2 minutes)

### Step 1: Copy Files

Copy the entire `UnityMCP` folder to your Unity project:

```
YourUnityProject/Assets/UnityMCP/
```

### Step 2: Wait for Compilation

Unity will automatically compile the scripts. Watch the Console for:

```
[McpToolRegistry] Registered 5 tools
[McpServer] Unity MCP Server v1.0.0 started on port 8765
```

✅ **Done!** The server is now running.

## Test Connection (1 minute)

### Option A: Python

Save this as `test.py`:

```python
import socket, json

sock = socket.socket()
sock.connect(('localhost', 8765))

# Ping test
request = {"jsonrpc": "2.0", "id": "1", "method": "ping", "params": {}}
sock.send((json.dumps(request) + "\n").encode())
print(sock.recv(1024).decode())

sock.close()
```

Run: `python test.py`

Expected output:
```json
{"jsonrpc":"2.0","id":"1","result":{"message":"pong","timestamp":"..."}}
```

### Option B: Command Line (Windows)

```powershell
echo '{"jsonrpc":"2.0","id":"1","method":"ping","params":{}}' | ncat localhost 8765
```

### Option C: Use Example Client

```bash
python example_client.py
```

## Your First Commands (2 minutes)

### 1. List Available Tools

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/list",
  "params": {}
}
```

**What you'll see**: List of 5 tools (ping, create_scene, create_gameobject, get_scene_info, create_script)

### 2. Create a Scene

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "create_scene",
  "params": {
    "name": "TestScene"
  }
}
```

**What happens**: New scene created at `Assets/Scenes/TestScene.unity`

### 3. Add a Cube

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "create_gameobject",
  "params": {
    "name": "MyCube",
    "type": "cube",
    "position": {"x": 0, "y": 1, "z": 0}
  }
}
```

**What happens**: Cube appears in scene at position (0, 1, 0)

### 4. Check Scene

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "method": "get_scene_info",
  "params": {
    "includeHierarchy": false
  }
}
```

**What you'll see**: Scene details including object count

### 5. Create a Script

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "method": "create_script",
  "params": {
    "name": "MyController",
    "type": "monobehaviour"
  }
}
```

**What happens**: `Assets/Scripts/MyController.cs` created

## Using the Example Clients

### Python Client

```bash
# Run the full example
python example_client.py

# Or use it as a library
from example_client import UnityMCPClient

client = UnityMCPClient()
client.connect()
client.ping()
client.create_scene("Level1")
client.close()
```

### Node.js Client

```bash
# Run the full example
node example_client.js

# Or use it as a module
const UnityMCPClient = require('./example_client');

const client = new UnityMCPClient();
await client.connect();
await client.ping();
await client.createScene('Level1');
client.close();
```

## Common Use Cases

### Automated Scene Setup

```python
client = UnityMCPClient()
client.connect()

# Create scene
client.create_scene("Level1", setup="default")

# Add environment
client.create_gameobject("Ground", "plane", {"x": 0, "y": 0, "z": 0})
client.create_gameobject("Sky", "sphere", {"x": 0, "y": 50, "z": 0})

# Add gameplay elements
client.create_gameobject("Player", "cube", {"x": 0, "y": 1, "z": 0})
client.create_gameobject("Enemy", "sphere", {"x": 5, "y": 1, "z": 0})

client.close()
```

### Script Generation Workflow

```python
client = UnityMCPClient()
client.connect()

# Generate player controller
client.create_script("PlayerController", "monobehaviour", 
                     namespace="Game.Player")

# Generate enemy AI
client.create_script("EnemyAI", "monobehaviour",
                     namespace="Game.Enemies")

# Generate game data
client.create_script("GameConfig", "scriptableobject",
                     namespace="Game.Data")

client.close()
```

### Scene Inspection

```python
client = UnityMCPClient()
client.connect()

# Get detailed scene info
info = client.get_scene_info(
    include_hierarchy=True,
    include_components=True
)

print(f"Scene: {info['result']['name']}")
print(f"Total objects: {info['result']['totalObjectCount']}")

# Print hierarchy
for obj in info['result']['rootObjects']:
    print(f"- {obj['name']} at {obj['position']}")

client.close()
```

## Troubleshooting

### "Connection refused"

**Problem**: Server not running  
**Solution**: Check Unity Console for startup message. Restart Unity if needed.

### "Tool not found"

**Problem**: Typo in method name  
**Solution**: Run `tools/list` to see available tools. Method names are case-sensitive.

### "Port already in use"

**Problem**: Another app using port 8765  
**Solution**: Change port in `McpServer.cs` or stop the other app.

### Unity freezes

**Problem**: Very long operation  
**Solution**: Tools run on main thread. Keep operations brief or use coroutines.

## Next Steps

📖 **Read the Docs**
- [README.md](README.md) - Full documentation
- [API_REFERENCE.md](API_REFERENCE.md) - Complete API
- [ARCHITECTURE.md](ARCHITECTURE.md) - How it works

🔧 **Customize**
- Create your own tools (see README)
- Modify existing tools
- Add authentication (for production)

🚀 **Build Cool Stuff**
- AI-assisted level design
- Automated testing
- Asset pipeline automation
- Custom editor workflows

## Getting Help

- Check Unity Console for error messages
- Review example clients for working code
- Read API reference for detailed specs
- Check troubleshooting section in README

## Safety Notes

⚠️ The server only accepts localhost connections by default (secure)  
⚠️ No authentication mechanism (don't expose to network)  
⚠️ Tools have full Unity Editor permissions (be careful with custom tools)

## Quick Reference

| Method | Purpose | Example |
|--------|---------|---------|
| `ping` | Test connection | `{"method":"ping"}` |
| `tools/list` | List tools | `{"method":"tools/list"}` |
| `create_scene` | New scene | `{"method":"create_scene","params":{"name":"Test"}}` |
| `create_gameobject` | New object | `{"method":"create_gameobject","params":{"name":"Cube","type":"cube"}}` |
| `get_scene_info` | Scene details | `{"method":"get_scene_info"}` |
| `create_script` | New script | `{"method":"create_script","params":{"name":"Test"}}` |

## Tips

💡 Always include `jsonrpc: "2.0"` and `id` in requests  
💡 Check response for `result` (success) or `error` (failure)  
💡 End messages with `\n` (newline)  
💡 Use `tools/list` to discover available tools  
💡 Start with `initialize` for proper MCP handshake

---

**You're all set!** Start building amazing Unity automations with MCP! 🎮✨
