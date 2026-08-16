---
name: unity-mcp
description: Unity MCP Server v3.0.0 — Model Context Protocol (MCP) server for Unity Editor automation. Built on the official C# MCP SDK. Enables AI agents to scaffold projects, create scenes, scripts, materials, prefabs, and manage assets with proper .meta sidecars; runs on Windows, Linux, and macOS (.NET 10).
---

# Unity MCP Server Skill


## Installation & Update

You can install the Unity MCP Server as a global .NET tool from **NuGet.org** or from a local build.

### From NuGet.org (recommended)

```shell
dotnet tool install --global UnityMCP.Server
```

Update to the latest published version:

```shell
dotnet tool update --global UnityMCP.Server
```

After installation, you can run the server from anywhere using:

```shell
unity-mcp
```

### From a local build (development)

From the repo root, either run:

```powershell
./install-tool.ps1
```

or the equivalent manual steps:

```shell
dotnet tool uninstall --global UnityMCP.Server
dotnet build Unity-MCP-Server.sln --configuration Release
dotnet pack UnityMCP.Server/UnityMCP.Server.csproj -c Release -o UnityMcp.Server/nupkg
dotnet tool update --global --add-source UnityMcp.Server/nupkg UnityMCP.Server
```

To test the tool with the official Inspector:

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
 - **Directory behavior**: All write-heavy tools (scaffold, create_scene, create_script, save_script/text/texture/audio, default scene, nav/input/anim/VFX/physics tools) automatically create any missing Unity subfolders under `projectPath` as needed (e.g. `Assets/Scenes`, `Assets/Scripts`, `Assets/Audio`, `Packages`).

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

- **C# Validation**: Roslyn syntax diagnostics plus type declaration checks
- **Import Validation**: Hybrid Editor bridge (live localhost or batch mode) when `UNITY_EDITOR_PATH` / open Editor is available; otherwise returns an explicit `ExternalTool` error
- **UPM Packages**: Add packages to `Packages/manifest.json` via JSON merge; Editor-backed search/resolve when bridge is available
- **Editor bridge**: `unity_install_editor_bridge` embeds `com.unitymcp.bridge` for native scene/component/asset operations

### 5. DevOps & CI/CD

- **Multi-platform builds**: Win64, OSX, Linux64, Android, iOS via Unity CLI batch mode
- **Error reporting**: Build failures surfaced to the agent

## 📖 Complete Tool Reference (80+ tools)

### 📡 Connectivity

| Tool | Description | Parameters |
|:---|:---|:---|
| `ping` | Health check | None |
| `unity_get_server_info` | Server version, transport, backend mode, Editor availability | None |
| `unity_get_capabilities` | Capability manifest for native/file-only/compatibility modes | None |

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
| `unity_scene_list_gameobjects` | List GameObjects/components in a scene or prefab | `projectPath`, `fileName` |
| `unity_scene_add_gameobject` | Add GO under parent path (Editor-backed; file-only appends root) | `projectPath`, `fileName`, `parentPath`, `objectName` |
| `unity_scene_reparent_gameobject` | Reparent GO (requires Editor) | `projectPath`, `fileName`, `objectPath`, `newParentPath` |
| `unity_scene_get_properties` / `unity_scene_set_properties` | Inspect/update GO transform properties | `projectPath`, `fileName`, `objectPath`, `propertiesJson?` |
| `unity_scene_set_active` | Enable/disable GameObject | `projectPath`, `fileName`, `objectPath`, `active` |
| `unity_component_list\|add\|remove\|get\|set\|set_enabled` | Component lifecycle (Editor-backed) | `projectPath`, `fileName`, `objectPath`, `componentType`, … |
| `unity_scene_rename_gameobject` | Rename a GameObject by name/path/fileID | `projectPath`, `fileName`, `objectPath`, `newName` |
| `unity_scene_remove_gameobject` | Remove a GameObject and directly referenced components | `projectPath`, `fileName`, `objectPath` |
| `unity_diff_scenes` | Structural diff between scenes/prefabs | `projectPath`, `fileNameA`, `fileNameB` |
| `unity_attach_script` | Attach a script as a MonoBehaviour (Editor-backed when available) | `projectPath`, `fileName`, `objectPath`, `scriptFileName` |
| `unity_instantiate_prefab` | PrefabUtility instantiate when Editor-backed; file-mode warning otherwise | `projectPath`, `sceneFileName`, `prefabFileName`, `instanceName` |
| `unity_save_gameobject_as_prefab` | Save an object from a scene into a prefab | `projectPath`, `sceneFileName`, `objectPath`, `prefabFileName` |
| `unity_install_editor_bridge` | Install `com.unitymcp.bridge` into the project | `projectPath` |

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
| `unity_get_asset_metadata` | Asset GUID/type/meta/dependency metadata | `projectPath`, `fileName` |
| `unity_list_asset_metadata` | Recursive asset metadata listing | `projectPath`, `folderName`, `pattern` |
| `unity_move_asset` | Move/rename asset and .meta sidecar | `projectPath`, `sourceFileName`, `destinationFileName` |
| `unity_update_material_properties` | Patch selected material YAML properties | `projectPath`, `fileName`, `propertiesJson` |
| `unity_assign_material_texture` | Assign texture GUID to material property | `projectPath`, `materialFileName`, `textureFileName`, `propertyName` |
| `unity_lint_project` | Static lint for missing .meta, broken GUIDs, missing scripts | `projectPath` |

### ✅ Validation & Packages

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_validate_csharp` | Roslyn syntax diagnostics → JSON result | `code` |
| `unity_add_packages` | Add UPM packages to manifest.json | `projectPath`, `packagesJson` |
| `unity_list_packages` | List manifest dependencies | `projectPath` |
| `unity_remove_packages` | Remove package IDs from manifest | `projectPath`, `packages` |
| `unity_verify_package_health` | Check manifest/lock health | `projectPath` |

### 🔌 MCP-Unity contract tools (JSON return)

All take `project_path` (absolute path to Unity project root). Return JSON for client parsing. Optional timeout applies when supported by transport.

| Tool | Description | Parameters | Return JSON |
|:---|:---|:---|:---|
| `unity_install_packages` | Install UPM packages by ID (add to manifest in order; default version if not sent) | `project_path`, `packages` (string[]) | `success`, `installed` (string[]), `message?` |
| `unity_create_default_scene` | Default scene: Main Camera (0,1,-10, Skybox), Directional Light (50,-30,0), Ground plane (5,1,5); scene + Ground.prefab | `project_path`, `scene_name` | `success`, `scene_path?`, `prefab_path?`, `message?` |
| `unity_configure_urp` | Linear color space, TagManager (tags Generated/AutoSetup, layers 8–9), default render pipeline | `project_path` | `success`, `message?` |
| `unity_validate_import` | Asset refresh + script compilation through Unity batch mode when `UNITY_EDITOR_PATH` is set | `project_path` | `success`, `error_count`, `warning_count`, `errors?`, `warnings?`, `message?` |

On failure for any tool: `success: false` and `message` (and tool-specific fields as applicable).

### 🏗️ Build

| Tool | Description | Key Parameters |
|:---|:---|:---|
| `unity_build_project` | Unity CLI batch build | `target`, `outputPath` |
| `unity_configure_project_settings` | Safe ProjectSettings sidecar and selected tag/layer patching | `projectPath`, `settingsJson` |
| `unity_configure_build_profile` | Build profile JSON asset under Assets/Settings | `projectPath`, `profileJson` |
| `unity_query_documentation` | Search local README, Docs, and Skills markdown | `query`, `maxResults` |
| `unity_camera_list` / `unity_camera_validate` | Camera domain listing/validation | `projectPath`, `folderName?` |
| `unity_light_list` / `unity_light_validate` | Light domain listing/validation | `projectPath`, `folderName?` |
| `unity_physics_list` / `unity_physics_validate` | Rigidbody/collider listing/validation | `projectPath`, `folderName?` |

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
