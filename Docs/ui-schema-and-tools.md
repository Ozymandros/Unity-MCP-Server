## UI Schema and Tools (Phase 1)

This document specifies the initial UI JSON schemas and MCP tools for UGUI-based UI authoring.
It is designed to be:

- **Opinionated but small**: focused on common menus and HUDs.
- **Extensible**: easy to grow in later phases without breaking contracts.
- **Aligned with clean architecture**: JSON contracts live in `UnityMcp.Core.Contracts`, tools in `UnityMcp.Application`, implementation in `UnityMcp.Infrastructure`.

---

## 1. JSON Contracts

The core UI contracts are defined in `UnityMcp.Core.Contracts.UiContracts`:

- `UiControlType`
- `UiAnchorPreset`
- `UiColor`
- `UiVector2`
- `UiRectTransform`
- `UiControl`
- `UiPanel`
- `UiLayout`

### 1.1 UiControlType

Supported control types (initial set):

- `Button`
- `Label`
- `Image`
- `Panel`

This list can be extended in future versions; unknown types should be treated as contract errors.

### 1.2 Layout primitives

`UiRectTransform` approximates a Unity `RectTransform` for high-level layout:

```json
{
  "anchor": "MiddleCenter",
  "anchoredPosition": { "x": 0, "y": 0 },
  "sizeDelta": { "x": 160, "y": 30 }
}
```

- `anchor`: one of `UiAnchorPreset` (e.g. `TopLeft`, `StretchFull`).
- `anchoredPosition`: pixel position relative to the anchor.
- `sizeDelta`: width / height.

### 1.3 UiControl

Represents an individual control:

```json
{
  "id": "startButton",
  "name": "StartButton",
  "type": "Button",
  "rect": {
    "anchor": "MiddleCenter",
    "anchoredPosition": { "x": 0, "y": -40 },
    "sizeDelta": { "x": 200, "y": 50 }
  },
  "text": "Start Game",
  "image": "Assets/Sprites/ButtonBackground.png",
  "onClickAction": "StartGame"
}
```

Notes:

- `id` is a logical identifier, useful for agents and code-behind mapping.
- `onClickAction` is an opaque string that callers can map to generated scripts or event wiring.

### 1.4 UiPanel

Panels are layout containers that can hold child panels and controls:

```json
{
  "id": "rootPanel",
  "name": "RootPanel",
  "rect": {
    "anchor": "StretchFull",
    "anchoredPosition": { "x": 0, "y": 0 },
    "sizeDelta": { "x": 0, "y": 0 }
  },
  "children": [],
  "controls": [ /* UiControl[] */ ]
}
```

### 1.5 UiLayout

Root object for the layout:

```json
{
  "name": "MainMenu",
  "panels": [
    {
      "id": "rootPanel",
      "name": "RootPanel",
      "rect": { "anchor": "StretchFull", "anchoredPosition": { "x": 0, "y": 0 }, "sizeDelta": { "x": 0, "y": 0 } },
      "children": [],
      "controls": [ /* ... */ ]
    }
  ]
}
```

This schema is versioned implicitly by the assembly; future breaking changes should introduce a v2 schema and companion tools.

---

## 2. MCP Tools

Two new tools are introduced for Phase 1 UI foundations.

### 2.1 unity_create_ui_canvas

**Signature**

- Server-side method: `IUnityService.CreateUiCanvasAsync(string projectPath, string fileName, CancellationToken)`
- MCP wrapper: `UnityTools.CreateUiCanvas`

**Description**

- Creates or updates a UGUI `Canvas` (with `EventSystem`) in a given scene or prefab.
- `fileName` can be:
  - A scene path: `Assets/Scenes/MainMenu.unity`
  - A prefab path: `Assets/Prefabs/MainMenuCanvas.prefab`
  - A bare name, which will be resolved via existing path rules.

**Phase 1 behavior**

- **New `.unity` file**: Writes a minimal scene (camera + light), then appends Canvas and EventSystem GameObjects (plain Transform, layer 5). Writes `.meta`, returns success JSON with project-relative `path`.
- **Existing scene**: Appends Canvas and EventSystem to the existing scene file.
- **Non-.unity (e.g. prefab)**: Writes a prefab containing only the Canvas GameObject.
- **Success JSON**: `{ "success": true, "path": "<project-relative path>", "message": null }`.

### 2.2 unity_create_ui_layout

**Signature**

- Server-side method: `IUnityService.CreateUiLayoutAsync(string projectPath, string fileName, string layoutJson, CancellationToken)`
- MCP wrapper: `UnityTools.CreateUiLayout`

**Description**

- Applies a high-level `UiLayout` JSON description (see schema above) to a Canvas stored in a scene or prefab.
- `layoutJson` must be a valid `UiLayout` JSON object.

**Phase 1 behavior**

- Parses `layoutJson` into `UiLayout`; on parse failure returns `ImportValidationResult` with `UnityMcpError` (code `UiLayout.InvalidJson`, category Validation).
- Builds a flat list of GameObjects from panels/controls via internal hierarchy builder; `UiRectTransform` anchored position is mapped to Transform position; all on layer 5. Appends their YAML fragments to the existing scene or prefab (file must already exist).
- Empty layout returns success with an `UiLayout.Empty` warning; otherwise returns success with message "UI layout applied successfully."
- Uses the same JSON result shape as validation tools: `success`, `error_count`, `warning_count`, `message`, optional `errors`/`warnings`.

**Future evolution**

- Add UGUI-specific components (Button, Text, Image) and full RectTransform mapping in generated YAML.

---

## 3. Architectural Alignment

- **Contracts**:
  - UI JSON schemas live in `UnityMcp.Core.Contracts` as plain DTOs with `JsonPropertyName` attributes.
- **Domain logic**:
  - `IUnityService` exposes UI operations independently of transport.
- **Infrastructure**:
  - `FileUnityService` will implement YAML generation in future phases.
- **Transport / MCP**:
  - `UnityTools` defines MCP tools using the official C# MCP SDK attributes.

This separation supports clean architecture and allows the UI layer to evolve without leaking YAML concerns into clients.

---

## 4. Recipes

### 4.1 unity_create_core_recipe

**Purpose**

- Single end-to-end MCP tool that runs: scaffold (or use existing project) → install URP + TextMeshPro → configure URP → create default scene → optionally add main menu UI (Canvas + minimal layout) → validate import. Intended for clients that want a ready-to-open Unity project in one call.

**MCP tool**

- `UnityTools.CreateCoreRecipe` (name: `unity_create_core_recipe`).

**Parameters**

| Parameter       | Type   | Default     | Description |
|----------------|--------|-------------|-------------|
| `projectName`  | string | `""`        | Name of the project when creating new; ignored if `projectPath` is set. |
| `outputRoot`   | string | `""`        | Output directory for new project (e.g. `C:\output`). Ignored if `projectPath` is set. |
| `projectPath`  | string | `""`        | Existing project root path. If set, scaffold is skipped. |
| `sceneName`    | string | `"MainScene"` | Name of the default scene (without `.unity`). |
| `includeMainMenu` | bool | `false`   | If true, creates `Assets/Scenes/MainMenu.unity` with Canvas and a minimal menu layout. |

**Order of steps**

1. Resolve path: use `projectPath` if provided; otherwise call `ScaffoldProjectAsync(projectName, outputRoot, null)`.
2. `InstallPackagesAsync` (URP, core, TextMeshPro).
3. `ConfigureUrpAsync`.
4. `CreateDefaultSceneAsync(projectPath, sceneName)`.
5. If `includeMainMenu`: `CreateUiCanvasAsync` for `Assets/Scenes/MainMenu.unity`, then `CreateUiLayoutAsync` with a minimal menu JSON.
6. `ValidateImportAsync`.

**Response JSON**

- `success` (bool): overall success (all steps succeeded).
- `projectPath` (string): resolved project root path.
- `scene_path` (string | null): from the default-scene step (e.g. `Assets/Scenes/MainScene.unity`).
- `steps` (array): per-step `{ name, success, message? }`.
- `message` (string | null): set when overall failure (e.g. missing projectName when not using projectPath).

**Example**

```json
{
  "success": true,
  "projectPath": "C:\\output\\MyGame",
  "scene_path": "Assets/Scenes/MainScene.unity",
  "steps": [
    { "name": "scaffold", "success": true, "message": null },
    { "name": "install_packages", "success": true, "message": null },
    { "name": "configure_urp", "success": true, "message": null },
    { "name": "create_default_scene", "success": true, "message": null },
    { "name": "validate_import", "success": true, "message": null }
  ],
  "message": null
}
```

If one step fails, `success` is false and the failing step has `success: false` and a `message`; remaining steps may still be present so clients can see partial progress.

