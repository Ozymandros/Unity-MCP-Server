---
name: unity-mcp
description: Unity MCP Server v3.0.0 — Model Context Protocol (MCP) server for Unity Editor automation. Built on the official C# MCP SDK. Enables AI agents to scaffold projects, create scenes, scripts, materials, prefabs, and manage assets with proper .meta sidecars; runs on Windows, Linux, and macOS (.NET 10).
---

# Unity MCP Server Skill


## Installation & Update

To install or update the Unity MCP Server as a global .NET tool:

1. Run the provided PowerShell script from the repo root:

  ```powershell
  ./install-tool.ps1
  ```

  This will:
  - Uninstall any existing global UnityMCP.Server tool
  - Build the solution in Release mode
  - Pack the server project (creates .nupkg in UnityMcp.Server/nupkg/)
  - Update the global tool from the local nupkg directory

  Equivalent manual steps (mutatis mutandis for your environment):
  ```shell
  dotnet tool uninstall --global UnityMCP.Server
  dotnet build Unity-MCP-Server.sln --configuration Release
  dotnet pack UnityMCP.Server/UnityMCP.Server.csproj -c Release -o nupkg
  dotnet tool update --global --add-source UnityMcp.Server/nupkg UnityMCP.Server
  ```

2. After installation, you can run the server from anywhere using:

  ```shell
  unity-mcp
  ```

3. To test the tool with the official Inspector:

  ```shell
  npx @modelcontextprotocol/inspector unity-mcp
  ```

# Unity MCP Server Skill

Pure .NET MCP server that bridges AI agents and Unity projects. Creates valid Unity YAML files (scenes, prefabs, materials) and proper `.meta` sidecars directly on disk. Uses the [official C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## 🚀 Core Capabilities

### 1. Project Scaffolding & Management

Create complete Unity project skeletons with all standard folders and `.meta` sidecars.
Unity requires `.meta` files next to every asset and folder — this server generates them automatically.

- **Scaffold**: Full project skeleton (Assets/, Scripts/, Textures/, Audio/, Text/, Scenes/, Prefabs/, Materials/, ProjectSettings/, Packages/)
- **Idempotent**: Reuses project folder by name — no timestamps or UUIDs
- **Project Info**: Query name, path, Unity version, asset status
- **Folders**: Create new folders with `.meta` sidecars

### 2. AI-Driven Scene Authoring

Create complete Unity scenes from JSON descriptions — the AI decides what GameObjects to place, with what components, transforms, and properties.

- **Detailed Scenes**: Camera, lights, geometry, colliders, rigidbodies — all via JSON→YAML
- **Incremental Building**: Add GameObjects to existing scenes one at a time
- **Prefabs**: Create reusable prefab assets with components
- **Materials**: PBR materials with color, metallic, smoothness, emission

### 3. Typed Asset Saving (with .meta)

Save AI-generated content into the correct Unity folder with the matching importer `.meta` sidecar:

- **Scripts** → `Assets/Scripts/` + MonoImporter `.meta`
- **Text** → `Assets/Text/` + DefaultImporter `.meta`
- **Textures** → `Assets/Textures/` + TextureImporter `.meta` (base64 PNG/JPG)
- **Audio** → `Assets/Audio/` + AudioImporter `.meta` (base64 MP3/WAV)

### 4. Validation & Packages

- **C# Validation**: Lightweight syntax checking (balanced braces/parens, class keyword)
- **UPM Packages**: Add packages to `Packages/manifest.json` via JSON merge

### 5. DevOps & CI/CD

- **Multi-platform builds**: Win64, OSX, Linux64, Android, iOS via Unity CLI batch mode
- **Error reporting**: Build failures surfaced to the agent

## 📖 Complete Tool Reference (26 tools)

### 📡 Connectivity

| Tool | Description | Parameters |
|:---|:---|:---|
| `ping` | Health check | None |

### 🏗️ Project Scaffolding

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_scaffold_project` | Create full project skeleton with .meta | `projectName`, `outputRoot?`, `unityVersion?` |
| `unity_get_project_info` | Get project metadata as JSON | `projectPath` |
| `unity_create_folder` | Create folder with .meta sidecar | `folderPath` |

### 🎬 Scene Authoring

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_scene` | Basic scene with default camera+light | `path` |
| `unity_create_detailed_scene` | Full scene from JSON GameObjects array | `path`, `sceneJson` |
| `unity_add_gameobject` | Append a GO to an existing scene | `scenePath`, `gameObjectJson` |
| `unity_create_gameobject` | Simple named GO (legacy) | `scenePath`, `gameObjectName` |

### 🧱 Asset Creation (with .meta sidecars)

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_script` | C# MonoBehaviour (+ MonoImporter .meta) | `path`, `scriptName`, `content?` |
| `unity_create_material` | PBR material (.mat) from JSON | `path`, `materialJson` |
| `unity_create_prefab` | Prefab (.prefab) from JSON | `path`, `prefabJson` |
| `unity_create_asset` | Generic text file (+ .meta) | `path`, `content` |

### 💾 Typed Asset Saving (project-relative + .meta)

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_save_script` | Save C# script → Assets/Scripts/ + MonoImporter .meta | `projectPath`, `fileName`, `content` |
| `unity_save_text` | Save text → Assets/Text/ + DefaultImporter .meta | `projectPath`, `fileName`, `content` |
| `unity_save_texture` | Save base64 image → Assets/Textures/ + TextureImporter .meta | `projectPath`, `fileName`, `base64Data` |
| `unity_save_audio` | Save base64 audio → Assets/Audio/ + AudioImporter .meta | `projectPath`, `fileName`, `base64Data` |

### 📂 File Operations

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_list_assets` | List files in directory | `path`, `pattern` |
| `unity_read_asset` | Read file content | `path` |
| `unity_delete_asset` | Delete file + .meta | `path` |

### ✅ Validation & Packages

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_validate_csharp` | Check C# syntax (braces, parens, class) → JSON result | `code` |
| `unity_add_packages` | Add UPM packages to manifest.json | `projectPath`, `packagesJson` |

### 🔌 MCP-Unity contract tools (JSON return)

All take `project_path` (absolute path to Unity project root). Return JSON for client parsing. Optional timeout applies when supported by transport.

| Tool | Description | Parameters | Return JSON |
|:---|:---|:---|:---|
| `unity_install_packages` | Install UPM packages by ID (add to manifest in order; default version if not sent) | `project_path`, `packages` (string[]) | `success`, `installed` (string[]), `message?` |
| `unity_create_default_scene` | Default scene: Main Camera (0,1,-10, Skybox), Directional Light (50,-30,0), Ground plane (5,1,5); scene + Ground.prefab | `project_path`, `scene_name` | `success`, `scene_path?`, `prefab_path?`, `message?` |
| `unity_configure_urp` | Linear color space, TagManager (tags Generated/AutoSetup, layers 8–9), default render pipeline | `project_path` | `success`, `message?` |
| `unity_validate_import` | Asset refresh + script compilation; errors and warnings (file-only stub: success, 0 counts until Unity batch integrated) | `project_path` | `success`, `error_count`, `warning_count`, `errors?`, `warnings?`, `message?` |

On failure for any tool: `success: false` and `message` (and tool-specific fields as applicable).

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

1. `unity_scaffold_project` → Create project skeleton with all folders
2. `unity_create_detailed_scene` → Create full scene with all objects
3. `unity_save_script` → Save AI-generated C# scripts (with .meta)
4. `unity_save_texture` → Save generated textures (with .meta)
5. `unity_save_audio` → Save generated audio (with .meta)
6. `unity_create_material` → Materials for each visual object
7. `unity_create_prefab` → Reusable object templates
8. `unity_validate_csharp` → Verify scripts before saving
9. `unity_add_packages` → Add UPM dependencies
10. `unity_list_assets` → Verify everything is in place

## 🔧 Setup

```powershell
./install-tool.ps1   # Install as global dotnet tool "unity-mcp"
```

No Unity DLLs required. The server writes Unity-compatible YAML directly.

## Adding this SKILL to a Client Project

From your client project directory, run:

```sh
npx skills add ../Unity-MCP-Server/Skills --skill SKILL
```

- Replace `../Unity-MCP-Server/Skills` with the actual path to your SKILL folder if different.
- Replace `SKILL` with the actual skill name if needed.

This will register the local SKILL with your client project, making it available for use with MCP-compatible tools and agents.

**Example:**
```sh
npx skills add ../Unity-MCP-Server/Skills --skill unity-mcp
```
