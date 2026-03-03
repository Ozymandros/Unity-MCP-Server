---
name: unity-mcp
description: Full-featured Model Context Protocol (MCP) server for Unity Editor automation. Built on the official C# MCP SDK. Enables AI agents to scaffold projects, create scenes, scripts, materials, prefabs, and manage assets with proper .meta sidecars — all without Unity DLLs.
---

# Unity MCP Server Skill


## Installation & Update

To install or update the Unity MCP Server as a global .NET tool:

1. Run the provided PowerShell script from the repo root:

  ```powershell
  ./install-tool.ps1
  ```

  This will:
  - Uninstall any existing global UnityMcp.Server tool
  - Build the solution in Release mode
  - Pack the UnityMcp.Server project (creates .nupkg in UnityMcp.Server/nupkg/)
  - Update the global tool from the local nupkg directory

  Equivalent manual steps (mutatis mutandis for your environment):
  ```shell
  dotnet tool uninstall --global UnityMcp.Server
  dotnet build UnityMcpServer.slnx --configuration Release
  dotnet pack UnityMcp.Server/UnityMcp.Server.csproj -c Release
  dotnet tool update --global --add-source UnityMcp.Server/nupkg UnityMcp.Server
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

### Path handling

- **projectPath**: Unity project root (camelCase). Use the path returned by `unity_scaffold_project` or an absolute path to an existing project.
- **fileName**: File name or path under the project (e.g. `Player.cs`, `Assets/Scripts/Player.cs`). Used for all file-oriented tools. The server validates both projectPath and fileName and builds the final I/O path so subfolder segments (e.g. `/Assets`, `/Scripts`) appear only once — no duplication.
- **folderName**: Folder name or path under the project (e.g. `Assets`, `Assets/Scripts`). Used for `unity_create_folder` and `unity_list_assets`. Same no-duplicate rule: final path = projectPath + folderName with overlapping segments stripped.

### 📡 Connectivity

| Tool | Description | Parameters |
|:---|:---|:---|
| `ping` | Health check | None |

### 🏗️ Project Scaffolding

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_scaffold_project` | Create full project skeleton with .meta | `projectName`, `outputRoot?`, `unityVersion?` |
| `unity_get_project_info` | Get project metadata as JSON | `projectPath` |
| `unity_create_folder` | Create folder with .meta sidecar | `projectPath`, `folderName` |

### 🎬 Scene Authoring

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_scene` | Basic scene with default camera+light | `projectPath`, `fileName` |
| `unity_create_detailed_scene` | Full scene from JSON GameObjects array | `projectPath`, `fileName`, `sceneJson` |
| `unity_add_gameobject` | Append a GO to an existing scene | `projectPath`, `fileName`, `gameObjectJson` |
| `unity_create_gameobject` | Simple named GO (legacy) | `projectPath`, `fileName`, `gameObjectName` |

### 🧱 Asset Creation (with .meta sidecars)

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_create_script` | C# MonoBehaviour (+ MonoImporter .meta) | `projectPath`, `fileName`, `scriptName`, `content?` |
| `unity_create_material` | PBR material (.mat) from JSON | `projectPath`, `fileName`, `materialJson` |
| `unity_create_prefab` | Prefab (.prefab) from JSON | `projectPath`, `fileName`, `prefabJson` |
| `unity_create_asset` | Generic text file (+ .meta) | `projectPath`, `fileName`, `content` |

### 💾 Typed Asset Saving (project-relative + .meta)

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_save_script` | Save C# script + MonoImporter .meta | `projectPath`, `fileName`, `content` |
| `unity_save_text` | Save text + DefaultImporter .meta | `projectPath`, `fileName`, `content` |
| `unity_save_texture` | Save base64 image + TextureImporter .meta | `projectPath`, `fileName`, `base64Data` |
| `unity_save_audio` | Save base64 audio + AudioImporter .meta | `projectPath`, `fileName`, `base64Data` |

### 📂 File Operations

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_list_assets` | List files in directory | `projectPath`, `folderName`, `pattern` |
| `unity_read_asset` | Read file content | `projectPath`, `fileName` |
| `unity_delete_asset` | Delete file + .meta | `projectPath`, `fileName` |

### ✅ Validation & Packages

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_validate_csharp` | Check C# syntax (braces, parens, class) → JSON result | `code` |
| `unity_add_packages` | Add UPM packages to manifest.json | `projectPath`, `packagesJson` |

### 🔌 MCP-Unity contract tools (JSON return)

All take `projectPath` (project root). Return JSON for client parsing.

| Tool | Description | Parameters | Return JSON |
|:---|:---|:---|:---|
| `unity_install_packages` | Install UPM packages by ID (add to manifest in order) | `projectPath`, `packages` (string[]) | `success`, `installed` (string[]), `message?` |
| `unity_create_default_scene` | Default scene: Main Camera, Directional Light, Ground plane; scene + Ground.prefab | `projectPath`, `sceneName` | `success`, `scene_path?`, `prefab_path?`, `message?` |
| `unity_configure_urp` | Linear color space, TagManager, default render pipeline | `projectPath` | `success`, `message?` |
| `unity_validate_import` | Asset refresh + script compilation; errors and warnings | `projectPath` | `success`, `error_count`, `warning_count`, `errors?`, `warnings?`, `message?` |

On failure for any tool: `success: false` and `message` (and tool-specific fields as applicable).

### 🏗️ Build

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_build_project` | Unity CLI batch build | `projectPath`, `target`, `outputPath` |

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

1. `unity_scaffold_project` → Create project skeleton; use returned path as `projectPath` for all following tools.
2. `unity_create_detailed_scene`(projectPath, fileName, sceneJson) → Create full scene.
3. `unity_save_script`(projectPath, fileName, content) → Save C# scripts (fileName = bare name or path; no duplicate segments).
4. `unity_save_texture` / `unity_save_audio` → Save generated assets.
5. `unity_create_material`(projectPath, fileName, materialJson) → Materials.
6. `unity_create_prefab`(projectPath, fileName, prefabJson) → Prefabs.
7. `unity_validate_csharp` → Verify scripts before saving.
8. `unity_add_packages`(projectPath, packagesJson) → Add UPM dependencies.
9. `unity_list_assets`(projectPath, folderName, pattern) → Verify everything is in place.

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
