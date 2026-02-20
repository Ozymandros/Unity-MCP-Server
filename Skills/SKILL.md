---
name: unity-mcp
description: Full-featured Model Context Protocol (MCP) server for Unity Editor automation. Built on the official C# MCP SDK. Enables AI agents to create detailed scenes, scripts, materials, prefabs, and manage project structure — all without Unity DLLs.
---

# Unity MCP Server Skill

Pure .NET MCP server that bridges AI agents and Unity projects. Creates valid Unity YAML files (scenes, prefabs, materials) directly on disk. Uses the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## 🚀 Core Capabilities

### 1. AI-Driven Scene Authoring

Create complete Unity scenes from JSON descriptions — the AI decides what GameObjects to place, with what components, transforms, and properties.

- **Detailed Scenes**: Camera, lights, geometry, colliders, rigidbodies — all via JSON→YAML
- **Incremental Building**: Add GameObjects to existing scenes one at a time
- **Prefabs**: Create reusable prefab assets with components
- **Materials**: PBR materials with color, metallic, smoothness, emission

### 2. Project Management

- **Discovery**: List and read files in the Assets tree
- **Code Generation**: MonoBehaviour scripts with correct class naming
- **Generic Assets**: Create any text file (shaders, configs, etc.)
- **Cleanup**: Delete files and their `.meta` sidecars

### 3. DevOps & CI/CD

- **Multi-platform builds**: Win64, OSX, Linux64, Android, iOS via Unity CLI batch mode
- **Error reporting**: Build failures surfaced to the agent

## 📖 Complete Tool Reference (13 tools)

### 📡 Connectivity

| Tool | Description | Parameters |
|:---|:---|:---|
| `ping` | Health check | None |

### 🎬 Scene Authoring

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_scene` | Basic scene with default camera+light | `path` |
| `unity_create_detailed_scene` | Full scene from JSON GameObjects array | `path`, `sceneJson` |
| `unity_add_gameobject` | Append a GO to an existing scene | `scenePath`, `gameObjectJson` |
| `unity_create_gameobject` | Simple named GO (legacy) | `scenePath`, `gameObjectName` |

### 🧱 Asset Creation

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_script` | C# MonoBehaviour | `path`, `scriptName` |
| `unity_create_material` | PBR material (.mat) from JSON | `path`, `materialJson` |
| `unity_create_prefab` | Prefab (.prefab) from JSON | `path`, `prefabJson` |
| `unity_create_asset` | Generic text file | `path`, `content` |

### 📂 File Operations

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_list_assets` | List files in directory | `path`, `pattern` |
| `unity_read_asset` | Read file content | `path` |
| `unity_delete_asset` | Delete file + .meta | `path` |

### 🏗️ Build

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_build_project` | Unity CLI batch build | `target`, `outputPath` |

## 📦 GameObject JSON Format

```json
{
  "name": "Player",
  "tag": "Player",
  "position": {"x": 0, "y": 1, "z": 0},
  "scale": {"x": 1, "y": 1, "z": 1},
  "eulerAngles": {"x": 0, "y": 90, "z": 0},
  "components": [
    {"type": "MeshFilter", "mesh": "Capsule"},
    {"type": "MeshRenderer"},
    {"type": "CapsuleCollider"},
    {"type": "Rigidbody", "mass": 2, "useGravity": true}
  ]
}
```

### Supported Component Types

| Type | Key Properties |
|:---|:---|
| `Camera` | `fov`, `nearClip`, `farClip`, `clearFlags`, `depth` |
| `Light` | `type` (0=Spot,1=Dir,2=Point), `intensity`, `range`, `color` |
| `MeshFilter` | `mesh` (Cube, Sphere, Capsule, Cylinder, Plane, Quad) |
| `MeshRenderer` | (auto default material) |
| `BoxCollider` | `size`, `center`, `isTrigger` |
| `SphereCollider` | `radius`, `center`, `isTrigger` |
| `CapsuleCollider` | `isTrigger` |
| `Rigidbody` | `mass`, `drag`, `angularDrag`, `useGravity`, `isKinematic` |
| `AudioSource` | `volume`, `loop`, `playOnAwake` |

### Material JSON Format

```json
{
  "name": "GoldMetal",
  "color": {"r": 1, "g": 0.84, "b": 0, "a": 1},
  "metallic": 0.9,
  "smoothness": 0.8,
  "emissionColor": {"r": 0.5, "g": 0.42, "b": 0, "a": 1},
  "renderMode": 0
}
```

## 💡 Recommended AI Workflow

1. `unity_create_detailed_scene` → Create full scene with all objects
2. `unity_create_script` → Generate player/enemy scripts
3. `unity_create_material` → Materials for each visual object
4. `unity_create_prefab` → Reusable object templates
5. `unity_list_assets` → Verify everything is in place
6. `unity_read_asset` → Inspect created files

## 🔧 Setup

```powershell
./install-tool.ps1   # Install as global dotnet tool "unity-mcp"
```

No Unity DLLs required. The server writes Unity-compatible YAML directly.
