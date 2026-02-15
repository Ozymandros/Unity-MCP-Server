
# Unity MCP Server Skill

This folder contains the official **MCP skill** for the Unity MCP Server.
It enables AI assistants (Claude, Copilot, Cursor, VS Code MCP clients, etc.) to interact with the Unity Editor through a TCP-based JSON-RPC interface.

---

## 🚀 Features Provided by This Skill

- Scene creation
- GameObject creation
- Script generation
- Scene inspection
- Connectivity testing

All operations are executed safely on Unity’s main thread via the server dispatcher.

---

## 📦 Files

- `unity-mcp-server.skill.json`: Skill manifest defining metadata, capabilities, and server connection details.

---

## 🧩 How to Use This Skill

### Claude Desktop / Claude Code
1. Open **Settings → MCP Servers**
2. Add a new server:
        - Type: `TCP`
        - Host: `localhost`
        - Port: `8765`

### VS Code MCP Client
1. Open **Settings → MCP Servers**
2. Add a new server:
        - Type: `TCP`
        - Host: `localhost`
        - Port: `8765`

---

## 🛠️ Available Tools

### 1. `ping`
**Description:** Checks connectivity with the Unity MCP Server.

**Input Schema:**
```json
{
    "message": "string (required)"
}
```

**Example:**
```json
{
    "tool": "ping",
    "input": { "message": "Hello" }
}
```

---

### 2. `create_scene`
**Description:** Creates a new Unity scene.

**Input Schema:**
```json
{
    "scene_name": "string (required)"
}
```
*Note: The API also supports optional parameters `path` and `setup` (see API reference).*

**Example:**
```json
{
    "tool": "create_scene",
    "input": { "scene_name": "TestScene" }
}
```

---

### 3. `create_gameobject`
**Description:** Creates a GameObject in the active scene.

**Input Schema:**
```json
{
    "name": "string (required)",
    "position": [number, number, number] (optional, length 3)
}
```
*Note: The API reference supports additional parameters: `type` (e.g., "cube", "sphere"), and `parent` (string). Position is usually an object: `{ "x": 0, "y": 1, "z": 0 }`.*

**Example:**
```json
{
    "tool": "create_gameobject",
    "input": { "name": "Cube", "position": [0, 1, 0] }
}
```

---

### 4. `get_scene_info`
**Description:** Returns information about the currently active scene.

**Input Schema:**
```json
{}
```
*Note: The API reference supports optional parameters: `includeHierarchy` (bool), `includeComponents` (bool).*

**Example:**
```json
{
    "tool": "get_scene_info",
    "input": {}
}
```

---

### 5. `create_script`
**Description:** Generates a C# script file inside the Unity project.

**Input Schema:**
```json
{
    "script_name": "string (required)",
    "content": "string (required)"
}
```
*Note: The API reference supports additional parameters: `type`, `path`, `namespace`.*

**Example:**
```json
{
    "tool": "create_script",
    "input": { "script_name": "PlayerController", "content": "// C# code here" }
}
```

---

## ⚠️ Notes & Best Practices

- **Parameter Consistency:** Some tool schemas in the skill manifest differ from the full API (see above notes). For advanced use, refer to the API reference for all supported parameters.
- **Thread Safety:** All Unity API calls are executed on the main thread via the dispatcher—no manual synchronization needed.
- **Error Handling:** Always check the response for `success` or error fields.
- **Initialization:** Always initialize the MCP connection before using tools.
- **Close Connections:** Properly close the connection when finished.

---

## 📚 Further Reference

- See `Documentation~/API_REFERENCE.md` for full protocol, tool schemas, and advanced usage examples.