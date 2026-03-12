## Phase 2 Schemas and Tools: Navigation, Input, Basic Animation

This document outlines the planned JSON schemas and MCP tools for Phase 2:

- Navigation (NavMesh and waypoint graphs)
- Modern Input System
- Basic animation (simple Animator graphs and clip binding)

The goal is to define **stable contracts and tool surfaces** that can be implemented
incrementally while keeping the architecture clean and extensible.

---

## 1. Navigation

### 1.1 JSON Schemas (planned)

#### NavMesh configuration

High-level schema for NavMesh bake settings:

```json
{
  "agentRadius": 0.5,
  "agentHeight": 2.0,
  "agentSlope": 45.0,
  "agentClimb": 0.4,
  "cellSize": 0.1,
  "cellHeight": 0.2,
  "manualVoxelSize": false
}
```

This schema will be mapped to a ScriptableObject or project-wide NavMesh settings asset
using a dedicated contract type (to be added in `UnityMcp.Core.Contracts`).

#### Waypoint graph

Representation for simple AI pathing:

```json
{
  "name": "PatrolRoute",
  "nodes": [
    { "id": "A", "position": { "x": 0, "y": 0, "z": 0 } },
    { "id": "B", "position": { "x": 5, "y": 0, "z": 0 } }
  ],
  "edges": [
    { "from": "A", "to": "B", "bidirectional": true }
  ]
}
```

Nodes and edges will be serialized as a ScriptableObject asset and consumed by
AI/navigation scripts.

### 1.2 Planned MCP Tools

- `unity_configure_navmesh`
  - Parameters:
    - `projectPath` (string)
    - `configJson` (NavMesh config JSON)
  - Behavior:
    - Writes/updates a NavMesh configuration asset in the project.
    - Optionally sets up components on designated surfaces in scenes.

- `unity_create_waypoint_graph`
  - Parameters:
    - `projectPath` (string)
    - `fileName` (asset path/name)
    - `graphJson` (waypoint graph JSON)
  - Behavior:
    - Creates a ScriptableObject asset representing the graph.

---

## 2. Modern Input System

### 2.1 JSON Schema (planned)

Simple `InputAction` asset representation:

```json
{
  "name": "PlayerControls",
  "maps": [
    {
      "name": "Gameplay",
      "actions": [
        { "name": "Move", "type": "Value", "expectedControlType": "Vector2" },
        { "name": "Jump", "type": "Button", "expectedControlType": "Button" }
      ],
      "bindings": [
        { "action": "Move", "path": "<Gamepad>/leftStick" },
        { "action": "Move", "path": "<Keyboard>/wasd" },
        { "action": "Jump", "path": "<Keyboard>/space" }
      ]
    }
  ]
}
```

This will be modeled by a set of DTOs under `UnityMcp.Core.Contracts` and translated
to Unity Input System JSON.

### 2.2 Planned MCP Tool

- `unity_create_input_actions`
  - Parameters:
    - `projectPath` (string)
    - `fileName` (asset path/name, e.g. `Assets/Input/PlayerControls.inputactions`)
    - `actionsJson` (InputAction JSON as above)
  - Behavior:
    - Generates a valid `.inputactions` asset on disk.
    - Future: Optionally creates a basic `PlayerInput` prefab wired to the asset.

---

## 3. Basic Animation

### 3.1 JSON Schema (planned)

Minimal description for an Animator Controller:

```json
{
  "name": "CharacterAnimator",
  "defaultState": "Idle",
  "states": [
    { "name": "Idle", "clip": "Assets/Animations/Idle.anim" },
    { "name": "Run", "clip": "Assets/Animations/Run.anim" }
  ],
  "transitions": [
    { "from": "Idle", "to": "Run", "condition": "Speed > 0.1" },
    { "from": "Run", "to": "Idle", "condition": "Speed <= 0.1" }
  ]
}
```

Constraints for Phase 2:

- Single-layer, humanoid or generic controllers only.
- Simple bool/float parameter conditions; no blend trees or sub-state machines yet.

### 3.2 Planned MCP Tool

- `unity_create_basic_animator`
  - Parameters:
    - `projectPath` (string)
    - `fileName` (Animator Controller asset path)
    - `animatorJson` (JSON as above)
  - Behavior:
    - Generates a simple Animator Controller asset.
    - Validates that referenced clips exist (via `unity_list_assets` or direct IO).

---

## 4. Contracts and Architecture

- JSON contracts for navigation, input, and basic animation will live under:
  - `UnityMcp.Core.Contracts` (e.g. `NavContracts`, `InputContracts`, `AnimationContracts`).
- `IUnityService` will grow methods:
  - `ConfigureNavmeshAsync`
  - `CreateWaypointGraphAsync`
  - `CreateInputActionsAsync`
  - `CreateBasicAnimatorAsync`
- `UnityTools` will expose MCP tools with matching signatures.
- Initial implementations in `FileUnityService` may be:
  - Partial (writing ScriptableObjects/JSON assets).
  - Or structured stubs returning clear messages, similar to the Phase 1 UI tools,
    until full YAML/asset generation is implemented.

This plan completes the Phase 2 design requirement by specifying the key JSON shapes
and tool entry points while preserving clean layering and backward-compatible evolution.

