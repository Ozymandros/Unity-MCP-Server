## Recipes and Scenarios

This document describes orchestration recipes and how clients (e.g. [Unity-Generator](https://github.com/Ozymandros/Unity-Generator)) can use them for one-click or guided flows.

---

## 1. Prototype project scenario

The **`unity_create_prototype_recipe`** MCP tool wires together a full Phase 2/3 prototype in one call:

1. **Project**: Either use an existing `projectPath` or scaffold a new project with `projectName` and `outputRoot`.
2. **Base setup**: Install URP + TextMeshPro, configure URP, create a default scene (e.g. `MainScene`).
3. **Optional Phase 2/3 features** (each controlled by a boolean):
   - **Main menu UI**: `Assets/Scenes/MainMenu.unity` with Canvas and minimal menu layout (same as core recipe).
   - **Nav**: NavMesh config + a default waypoint graph at `Assets/Data/PatrolRoute.waypoints.json`.
   - **Input**: Default input actions at `Assets/Input/PlayerControls.inputactions`.
   - **Animator**: Basic animator at `Assets/Animations/Character.animator.json`.
   - **Advanced animator**: Phase 3 advanced animator at `Assets/Animations/CharacterAdvanced.animator.json`.
   - **Timeline**: Phase 3 timeline at `Assets/Timelines/IntroCutscene.timeline.json`.
   - **VFX**: One particle-effect surrogate at `Assets/VFX/ExplosionSmall.vfx.json`.
   - **Physics**: One physics/ragdoll surrogate at `Assets/Physics/HumanoidRagdoll.physics.json`.
4. **Validation**: Run import validation on the project.
5. **Result**: A single JSON with `success`, `projectPath`, `scene_path`, and `steps[]` (per-step `name`, `success`, `message?`).

### How a client can call it

- **One-click “Full prototype”**: Call with `projectName` + `outputRoot`, and all optional flags (`includeMainMenu`, `includeNav`, `includeInput`, `includeAnimator`, `includeAdvancedAnimator`, `includeTimeline`, `includeVfx`, `includePhysics`) set to `true`. Parse the returned JSON; if `success` is true, use `projectPath` and `scene_path` for follow-up (e.g. open in Unity, run more tools).
- **Checkboxes / toggles**: Surface the eight optional flags as checkboxes (e.g. “Include main menu”, “Include nav”, “Include input”, “Include animator”, “Include advanced animator”, “Include timeline”, “Include VFX”, “Include physics”). Call the tool with the user’s choices; the same step-parsing logic as for `unity_create_core_recipe` applies (e.g. show which steps succeeded or failed).
- **Existing project**: Pass `projectPath` and leave `projectName` empty to add optional features to an already-created project; only the selected optional steps plus `validate_import` run after the base steps.

Contract details (parameters, result shape, error handling) are in [Tool contracts and observability (§6 Orchestration / Recipes)](tool-contracts-and-observability.md#6-orchestration--recipes).
