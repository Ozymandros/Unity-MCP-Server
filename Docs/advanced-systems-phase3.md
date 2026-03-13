## Phase 3: Advanced Animation, VFX, and Physics Systems

This document outlines the planned capabilities and MCP tools for Phase 3:

- Advanced animation (blend trees, layered state machines, timelines)
- VFX / particles
- Advanced physics setups (joints, ragdolls)

The emphasis is on:

- Clear separation between **high-level JSON contracts** and **Unity YAML/asset details**.
- Backward-compatible evolution from Phase 2 basic animation.

---

## 1. Advanced Animation

### 1.1 Capabilities

- Multi-layer Animator Controllers.
- Blend trees for movement (idle/walk/run).
- Sub-state machines for locomotion, combat, etc.
- Timeline sequences for cutscenes / scripted events.

### 1.2 JSON Contracts (implemented)

Contracts live under `UnityMcp.Core.Contracts.AnimationContracts` and build on
the Phase 2 basic animator schema:

- `AnimatorLayerContract`
- `AnimatorStateMachineContract`
- `AnimatorBlendTreeContract`
- `TimelineTrackContract`
- `TimelineClipContract`

These contracts are **Unity-agnostic JSON surrogates**: they are structured to map cleanly to Unity Animator Controllers and Timelines in the future, but in Phase 3 the server writes **`.json` assets plus `.meta` files**, not native `.controller` or Timeline assets.

Example (simplified):

```json
{
  "name": "CharacterAnimatorAdvanced",
  "layers": [
    {
      "name": "Base Layer",
      "defaultState": "Locomotion",
      "states": [ /* as in basic animator */ ],
      "subStateMachines": [
        {
          "name": "Locomotion",
          "defaultState": "Idle",
          "states": [ /* Idle, Walk, Run */ ],
          "transitions": [ /* intra-locomotion transitions */ ]
        }
      ]
    }
  ]
}
```

Timelines:

```json
{
  "name": "IntroCutscene",
  "tracks": [
    {
      "type": "Animation",
      "binding": "Player",
      "clips": [
        { "name": "IntroPose", "clip": "Assets/Animations/IntroPose.anim", "start": 0.0, "duration": 2.0 }
      ]
    },
    {
      "type": "Audio",
      "binding": "MusicSource",
      "clips": [
        { "name": "IntroMusic", "audio": "Assets/Audio/Intro.ogg", "start": 0.0, "duration": 30.0 }
      ]
    }
  ]
}
```

### 1.3 MCP Tools (implemented)

- **`unity_create_advanced_animator`**
  - Parameters: `projectPath`, `fileName` (e.g. `Assets/Animations/CharacterAdvanced.animator.json`), `animatorJson`.
  - Implementation: `IUnityService.CreateAdvancedAnimatorAsync` in `FileUnityService` validates the JSON against the advanced schema
    (layers with valid defaultState) and writes a JSON surrogate asset plus `.meta`.
  - Result JSON: `{ success, path, message, errors[] }`, with validation failures using codes such as `AdvancedAnimator.InvalidJson` and
    `AdvancedAnimator.InvalidLayer`.

- **`unity_create_timeline`**
  - Parameters: `projectPath`, `fileName` (e.g. `Assets/Timelines/IntroCutscene.timeline.json`), `timelineJson`.
  - Implementation: `IUnityService.CreateTimelineAsync` deserializes to `TimelineDefinition`, validates referenced animation/audio clip paths and
    writes a JSON surrogate asset plus `.meta`.
  - Result JSON: `{ success, path, message, errors[], warnings[] }`, with parse failures using `Timeline.InvalidJson` and missing assets reported
    as `Timeline.MissingClip` / `Timeline.MissingAudio` warnings.

---

## 2. VFX and Particles

### 2.1 Capabilities

- Generate common effects:
  - Muzzle flashes
  - Explosions
  - Weather (rain, snow)
  - Magic/energy effects
- Target built-in Particle System first; VFX Graph may be added later.

### 2.2 JSON Contracts (implemented)

Contracts under `UnityMcp.Core.Contracts.VfxContracts`:

- `ParticleEffectContract`
- `ParticleEmissionContract`
- `ParticleShapeContract`
- `ParticleColorOverLifetimeContract`

Example:

```json
{
  "name": "ExplosionSmall",
  "duration": 1.0,
  "looping": false,
  "startLifetime": 0.5,
  "startSpeed": 5.0,
  "startSize": 1.0,
  "startColor": { "r": 1, "g": 0.6, "b": 0.1, "a": 1 },
  "emission": {
    "rateOverTime": 0,
    "bursts": [
      { "time": 0.0, "count": 50 }
    ]
  },
  "shape": {
    "type": "Sphere",
    "radius": 0.5
  }
}
```

### 2.3 MCP Tool (implemented)

- **`unity_create_vfx_asset`**
  - Parameters:
    - `projectPath`
    - `fileName` (e.g. `Assets/VFX/ExplosionSmall.vfx.json`)
    - `vfxJson` (JSON matching `ParticleEffectContract`)
  - Implementation:
    - `IUnityService.CreateVfxAssetAsync` in `FileUnityService` deserializes `vfxJson` into `ParticleEffectContract`, performs basic semantic checks (non‑negative duration, valid burst counts, etc.), and writes a **JSON surrogate asset** plus `.meta`.
  - Result JSON:
    - Uses the `ImportValidationResult` pattern from `ToolContracts`:
      - `success: true | false`
      - `path`: project‑relative path to the generated `.vfx.json` asset (when created).
      - `message`: high‑level outcome summary.
      - `errors`: array of `UnityMcpError`.
    - Typical error codes:
      - `Vfx.InvalidJson` (category: `Validation`) when the payload cannot be parsed.
      - `Vfx.InvalidParameters` (category: `Validation`) for semantic issues such as negative durations or invalid emission settings.

---

## 3. Advanced Physics

### 3.1 Capabilities

- Pre-configured ragdoll setups for humanoid characters.
- Joint and constraint rigs for vehicles, doors, and mechanical objects.

### 3.2 JSON Contracts (implemented)

Contracts under `UnityMcp.Core.Contracts.PhysicsContracts`:

- `RagdollBoneContract`
- `RagdollSetupContract`
- `JointContract`

Example:

```json
{
  "name": "HumanoidRagdoll",
  "bones": [
    { "name": "Hips", "colliderType": "Capsule", "mass": 10.0 },
    { "name": "Spine", "colliderType": "Box", "mass": 8.0 }
  ]
}
```

### 3.3 MCP Tool (implemented)

- **`unity_create_physics_setup`**
  - Parameters:
    - `projectPath`
    - `fileName` (e.g. `Assets/Physics/HumanoidRagdoll.physics.json`)
    - `physicsJson` (JSON matching `RagdollSetupContract`)
  - Implementation:
    - `IUnityService.CreatePhysicsSetupAsync` in `FileUnityService` deserializes `physicsJson` into `RagdollSetupContract`, validates that bones and joints reference valid names, and writes a **JSON surrogate asset** plus `.meta`.
  - Result JSON:
    - Follows the same `ImportValidationResult`‑style shape used by other Phase 3 tools:
      - `success: true | false`
      - `path`: project‑relative path to the generated `.physics.json` asset (when created).
      - `message`: high‑level outcome summary.
      - `errors`: array of `UnityMcpError`.
    - Typical error codes:
      - `PhysicsSetup.InvalidJson` (category: `Validation`) when the payload cannot be parsed.
      - `PhysicsSetup.InvalidReference` (category: `Validation`) when joints or bones reference non‑existent names.

---

## 4. Testing and Compatibility

- All advanced systems rely on:
  - The shared `UnityMcpErrorCategory` / `UnityMcpError` taxonomy.
  - `ImportValidationResult`‑style envelopes for multi‑error responses (advanced animator, VFX, physics, timelines where applicable).
- Tools must:
  - Preserve **additive contracts** so that existing clients depending on Phase 2 navigation/input/animation continue to work.
  - Keep JSON schemas stable so future Unity‑native asset generation can reuse the same contracts and MCP tool names.

This document now reflects the **implemented** Phase 3 behavior: JSON‑level contracts, surrogate asset generation under `Assets/…/*.json`, and consistent result shapes and error codes for advanced animation, VFX, and physics tools.

---

## 5. Future work and Unity‑native mapping (post–Phase 3)

### 5.1 Contract-to-Unity mapping (design reference)

When Unity Editor or batch tooling is available, the existing JSON contracts can be mapped to real Unity assets. The following is a design reference for implementers; no Unity DLLs or runtime dependency are required in this repo.

- **Advanced animator**
  - **Contracts:** `AnimatorLayerContract`, `AnimatorStateMachineContract`, states, transitions, blend trees.
  - **Unity target:** Animator Controller asset (`.controller`). YAML includes `AnimatorStateMachine`, `AnimatorState`, `AnimatorStateTransition`, and (for blend trees) `BlendTree` with motion references. Layers map to top-level state machines; sub-state machines map to nested `ChildStateMachine` entries. Default state and transitions map to `defaultState` / `transitions` in the state machine serialization.

- **Timeline**
  - **Contracts:** `TimelineDefinition`, `TimelineTrackContract`, `TimelineClipContract` (animation clip path, audio path, start, duration).
  - **Unity target:** Timeline Playable Asset (e.g. `.playable` or Timeline-specific asset). YAML includes `PlayableAsset`, track bindings (binding to a GameObject or component), and clips with `start`, `duration`, and references to animation clips or audio assets. Animation tracks map to `AnimationPlayableAsset`; audio tracks to `AudioPlayableAsset`.

- **VFX**
  - **Contracts:** `ParticleEffectContract`, `ParticleEmissionContract`, `ParticleShapeContract`, color and burst settings.
  - **Unity target:** Built-in Particle System (component on a GameObject, or prefab). Serialization is component-based (ParticleSystem module data). Alternatively, VFX Graph can be targeted later with a separate mapping from the same contract to VFX Graph asset structure.

- **Physics**
  - **Contracts:** `RagdollSetupContract`, `RagdollBoneContract`, `JointContract` (bone names, collider types, mass, joint type and connected body).
  - **Unity target:** Prefab with hierarchy of GameObjects; each bone has a Rigidbody and Collider (Capsule/Box/Sphere from `colliderType`). Joints (Hinge, ConfigurableJoint, etc.) connect parent and child bones. Anchor and axis can be derived from bone names or default conventions.

MCP tool names and JSON request/response shapes remain unchanged; a future “Unity-native” writer (e.g. an Editor script or separate service that reads the same JSON and writes the above assets) can be plugged in or replace the current JSON-surrogate implementation when running inside or alongside Unity.

### 5.2 Other future work

- **Golden fixtures**: Implemented; advanced systems fixtures live under `UnityMcp.Tests/Fixtures/AdvancedSystems/` (see validation-pipeline-and-tests.md §4.3).
- **Extended recipe options**: Implemented; `unity_create_prototype_recipe` supports `includeMainMenu` and optional `packagesJson`. Template-based scene variants remain doc-only.

