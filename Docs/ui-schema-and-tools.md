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

- Currently returns a **structured JSON status** indicating that it is not yet implemented:

```json
{
  "success": false,
  "message": "unity_create_ui_canvas is not yet implemented in this build. Use scene and prefab authoring tools until UI support is released."
}
```

**Phase 1 goal**

- Define the contract and tool entry point so that clients can start integrating and writing orchestration flows against a stable name and parameter list.

### 2.2 unity_create_ui_layout

**Signature**

- Server-side method: `IUnityService.CreateUiLayoutAsync(string projectPath, string fileName, string layoutJson, CancellationToken)`
- MCP wrapper: `UnityTools.CreateUiLayout`

**Description**

- Applies a high-level `UiLayout` JSON description (see schema above) to a Canvas stored in a scene or prefab.
- `layoutJson` must be a valid `UiLayout` JSON object.

**Phase 1 behavior**

- Returns a structured placeholder result:

```json
{
  "success": false,
  "message": "unity_create_ui_layout is not yet implemented in this build. The UiLayout JSON contract is defined for upcoming releases."
}
```

**Future evolution**

- Will generate deterministic Unity YAML for UGUI objects:
  - Canvas, Panel hierarchy, Buttons, Text/Labels, Images.
  - RectTransform mappings derived from `UiRectTransform`.

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

