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

### 1.2 Planned JSON Contracts

Contracts will be added under `UnityMcp.Core.Contracts.AnimationContracts` and build on
the Phase 2 basic animator schema:

- `AnimatorLayerContract`
- `AnimatorStateMachineContract`
- `AnimatorBlendTreeContract`
- `TimelineTrackContract`
- `TimelineClipContract`

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

### 1.3 Planned MCP Tools

- `unity_create_advanced_animator`
  - Parameters:
    - `projectPath`
    - `fileName`
    - `animatorJson`
  - Behavior:
    - Generates a multi-layer Animator Controller from the advanced schema.
    - Can optionally migrate a basic animator (Phase 2) to the advanced form.

- `unity_create_timeline`
  - Parameters:
    - `projectPath`
    - `fileName`
    - `timelineJson`
  - Behavior:
    - Generates a Timeline asset with animation and audio tracks.

---

## 2. VFX and Particles

### 2.1 Capabilities

- Generate common effects:
  - Muzzle flashes
  - Explosions
  - Weather (rain, snow)
  - Magic/energy effects
- Target built-in Particle System first; VFX Graph may be added later.

### 2.2 Planned JSON Contracts

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

### 2.3 Planned MCP Tool

- `unity_create_vfx_asset`
  - Parameters:
    - `projectPath`
    - `fileName`
    - `vfxJson`
  - Behavior:
    - Generates a prefab or particle system asset configured from the contract.

---

## 3. Advanced Physics

### 3.1 Capabilities

- Pre-configured ragdoll setups for humanoid characters.
- Joint and constraint rigs for vehicles, doors, and mechanical objects.

### 3.2 Planned JSON Contracts

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

### 3.3 Planned MCP Tool

- `unity_create_physics_setup`
  - Parameters:
    - `projectPath`
    - `fileName`
    - `physicsJson`
  - Behavior:
    - Generates prefab(s) and/or components for ragdolls and joint rigs based on the JSON.

---

## 4. Testing and Compatibility

- All advanced systems rely on:
  - Golden YAML fixtures for complex assets.
  - Cross-version tests across at least two Unity LTS versions.
- Tools must:
  - Reuse the error taxonomy (`UnityMcpErrorCategory`) and error result patterns.
  - Prefer additive contracts; breaking changes should introduce new tool versions.

This outline satisfies the Phase 3 planning requirement by defining the target
capabilities, contract families, and MCP tools for advanced animation, VFX, and physics.

