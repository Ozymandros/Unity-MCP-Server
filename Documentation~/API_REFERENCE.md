# Unity MCP Server - API Reference

## MCP Protocol Methods

### initialize

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
    "clientInfo": {
      "name": "Client Name",
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

---

### tools/list

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
        "inputSchema": {
          "type": "object",
          "properties": {
            "message": {
              "type": "string",
              "description": "Optional message to echo"
            }
          }
        }
      }
    ]
  }
}
```

---

### tools/call

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
    "arguments": {
      "name": "MyScene"
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "call",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"success\":true,\"path\":\"Assets/Scenes/MyScene.unity\"}"
      }
    ]
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
  "params": {
    "message": "Hello"
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
    "echo": "Hello",
    "timestamp": "2025-02-14T10:30:00.000Z"
  }
}
```

---

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
  "params": {
    "name": "MainMenu",
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
    "path": "Assets/Scenes/MainMenu.unity",
    "name": "MainMenu",
    "setup": "default",
    "objectCount": 2,
    "message": "Scene 'MainMenu' created successfully at Assets/Scenes/MainMenu.unity"
  }
}
```

**Error Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "result": {
    "success": false,
    "error": "Scene name cannot be empty"
  }
}
```

---

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
  "params": {
    "name": "Player",
    "type": "cube",
    "position": {
      "x": 0,
      "y": 1,
      "z": 0
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "result": {
    "success": true,
    "name": "Player",
    "instanceId": 12345,
    "type": "cube",
    "position": {
      "x": 0.0,
      "y": 1.0,
      "z": 0.0
    },
    "message": "GameObject 'Player' created successfully"
  }
}
```

**Primitive Types**:
- `empty`: Empty GameObject with Transform only
- `cube`: Cube primitive with BoxCollider and MeshRenderer
- `sphere`: Sphere primitive with SphereCollider and MeshRenderer
- `capsule`: Capsule primitive with CapsuleCollider and MeshRenderer
- `cylinder`: Cylinder primitive with MeshCollider and MeshRenderer
- `plane`: Plane primitive with MeshCollider and MeshRenderer
- `quad`: Quad primitive with MeshCollider and MeshRenderer

---

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
  "params": {
    "includeHierarchy": true,
    "includeComponents": false
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "result": {
    "success": true,
    "name": "SampleScene",
    "path": "Assets/Scenes/SampleScene.unity",
    "isLoaded": true,
    "isDirty": false,
    "buildIndex": 0,
    "rootCount": 3,
    "totalObjectCount": 5,
    "rootObjects": [
      {
        "name": "Main Camera",
        "tag": "MainCamera",
        "layer": "Default",
        "active": true,
        "static": false,
        "instanceId": 1234,
        "position": {"x": 0, "y": 1, "z": -10}
      }
    ]
  }
}
```

**With Components**:
```json
{
  "includeComponents": true
}
```

Response includes:
```json
{
  "components": [
    "Transform",
    "Camera",
    "AudioListener"
  ]
}
```

---

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
  "params": {
    "name": "PlayerController",
    "type": "monobehaviour",
    "path": "Assets/Scripts/Player",
    "namespace": "Game.Controllers"
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": "5",
  "result": {
    "success": true,
    "path": "Assets/Scripts/Player/PlayerController.cs",
    "name": "PlayerController",
    "type": "monobehaviour",
    "message": "Script 'PlayerController' created successfully at Assets/Scripts/Player/PlayerController.cs"
  }
}
```

**Script Types**:

- `monobehaviour`: MonoBehaviour with Start() and Update()
- `scriptableobject`: ScriptableObject with CreateAssetMenu attribute
- `plain`: Plain C# class
- `interface`: Interface definition

**Generated MonoBehaviour**:
```csharp
using System;
using UnityEngine;

namespace Game.Controllers
{
    /// <summary>
    /// PlayerController MonoBehaviour
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        void Start()
        {
            // Initialization code here
        }

        void Update()
        {
            // Update code here
        }
    }
}
```

---

## Error Codes

Standard JSON-RPC 2.0 error codes:

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
  "error": {
    "code": -32601,
    "message": "Method not found: unknown_method",
    "data": "Additional error information"
  }
}
```

---

## Data Types

### Vector3 Position
```json
{
  "x": 0.0,
  "y": 1.0,
  "z": 0.0
}
```

### GameObject Info
```json
{
  "name": "GameObject",
  "tag": "Untagged",
  "layer": "Default",
  "active": true,
  "static": false,
  "instanceId": 12345,
  "position": {"x": 0, "y": 0, "z": 0},
  "components": ["Transform", "Rigidbody"],
  "children": [/* nested GameObjects */]
}
```

### Scene Info
```json
{
  "name": "SceneName",
  "path": "Assets/Scenes/SceneName.unity",
  "isLoaded": true,
  "isDirty": false,
  "buildIndex": 0,
  "rootCount": 3,
  "totalObjectCount": 10,
  "rootObjects": [/* GameObject info */]
}
```

---

## Usage Patterns

### Sequential Operations

Creating a scene and populating it:

```python
# 1. Create scene
client.send_request("create_scene", {"name": "Level1"})

# 2. Create environment
client.send_request("create_gameobject", {
    "name": "Ground",
    "type": "plane",
    "position": {"x": 0, "y": 0, "z": 0}
})

# 3. Create player
client.send_request("create_gameobject", {
    "name": "Player",
    "type": "cube",
    "position": {"x": 0, "y": 1, "z": 0}
})

# 4. Verify scene
info = client.send_request("get_scene_info", {})
```

### Hierarchical Objects

Creating parent-child relationships:

```python
# Create parent
client.send_request("create_gameobject", {
    "name": "Enemy",
    "type": "sphere"
})

# Create child
client.send_request("create_gameobject", {
    "name": "WeaponSlot",
    "type": "empty",
    "parent": "Enemy"
})
```

### Script Generation Workflow

```python
# 1. Create script
client.send_request("create_script", {
    "name": "EnemyAI",
    "type": "monobehaviour",
    "namespace": "Game.AI"
})

# 2. Wait for Unity to compile
time.sleep(2)

# 3. Use the script (manual attachment in Unity)
```

---

## Rate Limiting

No built-in rate limiting. Clients should:
- Wait for response before sending next request
- Implement exponential backoff on errors
- Batch operations when possible

## Thread Safety

All Unity API calls automatically execute on the main thread via McpDispatcher. No manual thread synchronization needed in tools.

## Best Practices

1. **Always initialize** before using other methods
2. **Check response.result** for success/failure
3. **Handle errors** appropriately
4. **Close connections** when done
5. **Validate parameters** before sending
6. **Use appropriate types** for each tool
