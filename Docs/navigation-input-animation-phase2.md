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

Contracts are in `UnityMcp.Core.Contracts.NavContracts` (`NavMeshConfig`). Phase 2 implementation
writes a JSON asset to `Assets/Settings/NavMeshConfig.json` (ScriptableObject surrogate).
Result JSON: `success`, `path`, `message`, `errors` (validation failures use code `NavMeshConfig.InvalidJson`).

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

Contracts: `WaypointGraph`, `WaypointNode`, `WaypointEdge`, `NavVector3` in `NavContracts`.
Phase 2 writes a JSON asset (e.g. `Assets/Data/PatrolRoute.waypoints.json`). Edges must reference
existing node ids; invalid edges return validation error with code `WaypointGraph.InvalidEdge`.
Result JSON: `success`, `path`, `message`, `errors`.

### 1.2 MCP Tools (implemented)

- **`unity_configure_navmesh`**
  - Parameters: `projectPath`, `configJson` (NavMeshConfig JSON).
  - Writes `Assets/Settings/NavMeshConfig.json` and `.meta`. Returns JSON: success, path, message, errors.

- **`unity_create_waypoint_graph`**
  - Parameters: `projectPath`, `fileName` (e.g. `Assets/Data/PatrolRoute.waypoints.json`), `graphJson`.
  - Validates node ids referenced by edges; writes JSON asset and `.meta`. Returns JSON: success, path, message, errors.

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

Contracts: `InputActionsAsset`, `InputActionMapDef`, `InputActionDef`, `InputBindingDef` in `UnityMcp.Core.Contracts.InputContracts`. Phase 2 writes JSON to the given path (e.g. `Assets/Input/PlayerControls.inputactions`). Result JSON: `success`, `path`, `message`, `errors` (parse failure uses code `InputActions.InvalidJson`). No `PlayerInput` prefab is created yet.

### 2.2 MCP Tool (implemented)

- **`unity_create_input_actions`**
  - Parameters: `projectPath`, `fileName` (e.g. `Assets/Input/PlayerControls.inputactions`), `actionsJson`.
  - Writes JSON asset and `.meta`. Returns JSON: success, path, message, errors.

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

Constraints for Phase 2: single-layer, simple bool/float conditions; no blend trees or sub-state machines. Contracts: `BasicAnimatorDefinition`, `AnimatorStateDef`, `AnimatorTransitionDef` in `UnityMcp.Core.Contracts.AnimationContracts`. Phase 2 writes a JSON surrogate (e.g. `Assets/Animations/Character.animator.json`) and validates that referenced clip paths exist under the project; missing clips are reported as warnings. Result JSON: `success`, `path`, `message`, `errors`, `warnings` (codes include `BasicAnimator.InvalidJson`, `BasicAnimator.MissingClip`).

### 3.2 MCP Tool (implemented)

- **`unity_create_basic_animator`**
  - Parameters: `projectPath`, `fileName` (e.g. `Assets/Animations/Character.animator.json`), `animatorJson`.
  - Writes JSON definition and `.meta`; missing clip paths add warnings. Returns JSON: success, path, message, errors, warnings.

---

## 4. Contracts and Architecture (implemented)

- **Contracts**: `UnityMcp.Core.Contracts.NavContracts`, `InputContracts`, `AnimationContracts` define the JSON DTOs.
- **IUnityService** methods: `ConfigureNavmeshAsync`, `CreateWaypointGraphAsync`, `CreateInputActionsAsync`, `CreateBasicAnimatorAsync`.
- **UnityTools** MCP tools: `unity_configure_navmesh`, `unity_create_waypoint_graph`, `unity_create_input_actions`, `unity_create_basic_animator`.
- **FileUnityService** writes JSON assets (NavMesh config, waypoint graphs, input actions, animator definition surrogates) and `.meta` files; validation errors use `UnityMcpError` and optional `ImportValidationResult`-style payloads.

Phase 2 implementations are Unity-agnostic (no Unity DLLs); assets are JSON-backed and can be extended later to native Unity formats.

