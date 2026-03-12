## Unity MCP Tool Contracts and Observability (Phase 0)

This document formalizes the **JSON contracts**, **error taxonomy**, and **observability requirements** for the Unity MCP server.
It is designed to be **backwards compatible** with existing tools while adding structure and clarity.

---

## 1. Error Taxonomy and Standard Error Shape

All Unity MCP operations that can fail in a structured way SHOULD use the shared error model defined in `UnityMcp.Core.Contracts`:

- `UnityMcpErrorCategory`:
  - `Validation` – invalid user input, schema violations, missing required project assets.
  - `Io` – filesystem errors (missing files, permission issues, disk problems).
  - `Contract` – mismatched expectations between client and server contracts (e.g. unsupported fields).
  - `ExternalTool` – failures coming from Unity CLI or other external tools.
  - `Internal` – unexpected exceptions or bugs within the server.

- `UnityMcpError` JSON shape:

  ```json
  {
    "category": "Validation | Io | Contract | ExternalTool | Internal",
    "code": "InstallPackages.Failure",
    "message": "Human-readable description",
    "details": { "optional": "opaque context" }
  }
  ```

**Guidelines**

- `code` MUST be:
  - Stable across versions for the same logical error.
  - Namespaced by feature, e.g. `"ConfigureUrp.MissingRenderPipelineAsset"`.
- `message` SHOULD be:
  - Clear to both agents and humans.
  - Free of sensitive information (paths may be included where useful).
- `details` MAY contain:
  - Exception types, stack traces, or environment info – intended for logs and debugging, not for stable contracts.

---

## 2. Standard Result Types (Phase 0 Coverage)

The following result types are defined under `UnityMcp.Core.Contracts` and used by `FileUnityService` for JSON-returning operations.
Property names map directly to JSON fields.

### 2.1 InstallPackagesResult

**Used by**

- `IUnityService.InstallPackagesAsync`
- MCP tool: `unity_install_packages`

**C# type**

- `UnityMcp.Core.Contracts.InstallPackagesResult`

**JSON shape**

```json
{
  "success": true,
  "installed": ["com.unity.render-pipelines.universal"],
  "message": null,
  "errors": [
    {
      "category": "Internal",
      "code": "InstallPackages.Failure",
      "message": "Error message",
      "details": { /* optional */ }
    }
  ]
}
```

**Compatibility notes**

- Existing clients that only read `success`, `installed`, and `message` continue to work.
- `errors` is additive and optional.

### 2.2 DefaultSceneResult

**Used by**

- `IUnityService.CreateDefaultSceneAsync`
- MCP tool: `unity_create_default_scene`

**C# type**

- `UnityMcp.Core.Contracts.DefaultSceneResult`

**JSON shape**

```json
{
  "success": true,
  "scene_path": "Assets/Scenes/MainScene.unity",
  "prefab_path": "Assets/Prefabs/Ground.prefab",
  "message": null,
  "errors": []
}
```

On failure:

```json
{
  "success": false,
  "scene_path": null,
  "prefab_path": null,
  "message": "Failed to create default scene",
  "errors": [
    {
      "category": "Internal",
      "code": "CreateDefaultScene.Failure",
      "message": "Exception message"
    }
  ]
}
```

### 2.3 UrpConfigurationResult

**Used by**

- `IUnityService.ConfigureUrpAsync`
- MCP tool: `unity_configure_urp`

**C# type**

- `UnityMcp.Core.Contracts.UrpConfigurationResult`

**JSON shape**

Success:

```json
{
  "success": true,
  "message": null,
  "errors": []
}
```

Missing URP asset (validation failure):

```json
{
  "success": false,
  "message": "No RenderPipelineAsset found in project. Add URP package and create or import a pipeline asset.",
  "errors": [
    {
      "category": "Validation",
      "code": "ConfigureUrp.MissingRenderPipelineAsset",
      "message": "No RenderPipelineAsset found in project. Add URP package and create or import a pipeline asset."
    }
  ]
}
```

Unexpected failure:

```json
{
  "success": false,
  "message": "Exception message",
  "errors": [
    {
      "category": "Internal",
      "code": "ConfigureUrp.Failure",
      "message": "Exception message"
    }
  ]
}
```

### 2.4 ImportValidationResult

**Used by**

- `IUnityService.ValidateImportAsync`
- MCP tool: `unity_validate_import`

**C# type**

- `UnityMcp.Core.Contracts.ImportValidationResult`

**JSON shape (Phase 0 stub implementation)**

```json
{
  "success": true,
  "error_count": 0,
  "warning_count": 0,
  "errors": [],
  "warnings": [],
  "message": "Stub: file-only server cannot run Unity compilation. Implement batch-mode validation when Unity is available."
}
```

**Future (Phase 1+) behavior**

- `error_count` and `warning_count` MUST match the lengths of `errors` and `warnings`.
- Each error/warning SHOULD carry a structured `UnityMcpError` with appropriate `category` and `code`, e.g.:
  - `"Validation"` for script/compiler errors and import issues.
  - `"ExternalTool"` for Unity CLI failures.

---

## 3. Observability Requirements (Phase 0)

Phase 0 focuses on establishing **minimum observability guarantees** without introducing heavy dependencies.

### 3.1 Logging

- All domain-level operations in `FileUnityService` MUST:
  - Log key actions with `ILogger<FileUnityService>` using structured templates.
  - Use **information-level logs** for successful operations:
    - Examples:
      - `"Project scaffolded at {Path}"`
      - `"Script saved at {Path} (with MonoImporter .meta)"`
      - `"Installed {Count} packages at {Path}"`
  - Use **warning-level logs** for recoverable failures:
    - `"InstallPackages failed for {Path}"`
    - `"ConfigureUrp failed for {Path}"`

- Logging configuration (in `UnityMCP.Server/Program.cs`):
  - Routes logs to **stderr** to avoid interfering with MCP stdio protocol.
  - Uses a minimum level of `Warning` by default; tooling / CI can raise verbosity when debugging.

### 3.2 Correlation and Structure

- Log messages should:
  - Include **projectPath** or other key identifiers where relevant.
  - Use **structured logging placeholders** (`{Path}`, `{Count}`) rather than string concatenation.

- In future phases (beyond initial Phase 0 implementation), we may introduce:
  - Explicit **correlation IDs** per MCP request.
  - Scoped logging contexts (e.g. using `using var scope = _logger.BeginScope(...)`).

### 3.3 Metrics (Planned)

While Phase 0 does not yet wire a metrics backend, the following conceptual metrics are defined and should be added when a metrics provider is available:

- Counters:
  - `unity_mcp_tool_invocations_total{toolName, outcome}` – success/failure per MCP tool.
  - `unity_mcp_validation_errors_total{category}` – count of validation errors by category.
- Histograms:
  - `unity_mcp_tool_latency_seconds{toolName}` – distribution of execution time per tool.

---

## 4. Contract Governance Principles

- **Additive evolution**:
  - New fields (such as `errors`) are added in a way that does not break existing clients.
- Existing top-level fields (`success`, `message`, `installed`, `scene_path`, `prefab_path`, etc.) are preserved.

- **Versioning & deprecation**:
  - If a breaking change is necessary for a tool’s contract, introduce a **new operation version** (e.g. `unity_validate_import_v2`) and deprecate the old one gradually.
  - Document deprecations in both:
    - Skill documentation (`Skills/…/SKILL.md`)
    - Release notes / change logs.

- **Testing**:
  - Contract tests MUST assert:
    - Presence and meaning of required fields.
    - Backwards compatibility for legacy clients where applicable.
  - Golden JSON fixtures SHOULD be used for the result types defined in this document, especially around edge cases (errors, partial success).

This document, together with the shared contract types in `UnityMcp.Core.Contracts`, satisfies Phase 0 requirements for tool contracts, error taxonomy, and baseline observability.

---

## 6. Orchestration / Recipes

The **`unity_create_core_recipe`** MCP tool returns a JSON summary that reuses the same success/step pattern:

- **Top-level**: `success`, `projectPath`, `scene_path`, `steps`, `message`.
- **steps[]**: array of `{ name, success, message? }` for each step (scaffold, install_packages, configure_urp, create_default_scene, optional create_ui_canvas/create_ui_layout, validate_import).

The **`unity_create_prototype_recipe`** MCP tool is a Phase 2/3 project bootstrapper with the same JSON result shape. It resolves the project (existing `projectPath` or scaffold via `projectName` + `outputRoot`), then runs: install_packages, configure_urp, create_default_scene; optionally (when flags are true) configure_navmesh, create_waypoint_graph, create_input_actions, create_basic_animator, create_vfx_asset, create_physics_setup; and always validate_import last.

- **Parameters**: `projectName`, `outputRoot`, `projectPath`, `sceneName` (default `"MainScene"`), `includeNav`, `includeInput`, `includeAnimator`, `includeVfx`, `includePhysics` (all optional booleans, default false).
- **Result**: Same as core recipe — `success`, `projectPath`, `scene_path`, `steps[]` (with per-step `name`, `success`, `message?`), `message`. Clients can reuse the same step-parsing logic as for `unity_create_core_recipe`.

Full parameter list, step order, and example response for the core recipe are documented in [UI Schema and Tools (§4 Recipes)](ui-schema-and-tools.md#4-recipes).

---

## 7. Phase 2 tool result shapes (Navigation, Input, Animation)

Phase 2 tools return JSON with a consistent pattern:

- **Success**: `success: true`, `path` (project-relative asset path), `message`, `errors: []`. Animation may include `warnings: []` for missing clip references.
- **Validation failure**: `success: false`, optional `path`, `message`, and `errors` array of `UnityMcpError` with stable codes:
  - Navigation: `NavMeshConfig.InvalidJson`, `WaypointGraph.InvalidJson`, `WaypointGraph.InvalidEdge`.
  - Input: `InputActions.InvalidJson`.
  - Animation: `BasicAnimator.InvalidJson`; `BasicAnimator.MissingClip` appears in `warnings` when the asset is still created.

Input and animation validation failures may use the same `ImportValidationResult` shape (`error_count`, `warning_count`, `errors`, `warnings`) for consistency. Full schemas and tool parameters are in [Navigation, Input, Animation (Phase 2)](navigation-input-animation-phase2.md).

---

## 8. Phase 3 tool result shapes (Advanced Animation, VFX, Physics)

Phase 3 extends the same patterns to **advanced animation**, **VFX/particles**, and **advanced physics** tools. These tools all operate on **JSON surrogate assets** (e.g. `.animator.json`, `.timeline.json`, `.vfx.json`, `.physics.json`) and reuse the shared error taxonomy.

### 8.1 Advanced animation tools

**Tools**

- `unity_create_advanced_animator` → `IUnityService.CreateAdvancedAnimatorAsync`
- `unity_create_timeline` → `IUnityService.CreateTimelineAsync`

**Common patterns**

- On success:
  - `success: true`
  - `path`: project-relative path to the generated JSON surrogate asset.
  - `message`: optional success message.
  - `errors: []`
- On validation / parse failure:
  - `success: false`
  - `path`: optional (may be omitted if no asset is written).
  - `message`: summary of the failure.
  - `errors`: array of `UnityMcpError` with Phase 3-specific `code` values.

**Advanced animator**

- Uses an `ImportValidationResult`-style envelope when multiple structural issues are possible (e.g. invalid layers, inconsistent blend trees).
- Typical error codes:
  - `AdvancedAnimator.InvalidJson` (category: `Validation`) when `animatorJson` cannot be parsed.
  - `AdvancedAnimator.InvalidLayer` (category: `Validation`) when a layer’s `defaultState` is missing or references an unknown state/sub-state machine.

**Timelines**

- Result JSON:

  ```json
  {
    "success": true,
    "path": "Assets/Timelines/IntroCutscene.timeline.json",
    "message": "Timeline created.",
    "errors": [],
    "warnings": []
  }
  ```

- Typical codes:
  - `Timeline.InvalidJson` (category: `Validation`) when `timelineJson` cannot be parsed.
  - `Timeline.MissingClip`, `Timeline.MissingAudio` (usually emitted as `warnings` when referenced assets are not found under `Assets` but the timeline is still created).

For full contract schemas, see [Phase 3: Advanced Animation, VFX, and Physics Systems](advanced-systems-phase3.md#1-advanced-animation).

### 8.2 VFX / particles tool

**Tool**

- `unity_create_vfx_asset` → `IUnityService.CreateVfxAssetAsync`

**Result envelope**

- Follows the `ImportValidationResult` pattern:

  ```json
  {
    "success": true,
    "path": "Assets/VFX/ExplosionSmall.vfx.json",
    "message": "VFX asset created.",
    "errors": [],
    "error_count": 0,
    "warning_count": 0,
    "warnings": []
  }
  ```

- Typical error codes:
  - `Vfx.InvalidJson` (category: `Validation`) when `vfxJson` cannot be parsed into `ParticleEffectContract`.
  - `Vfx.InvalidParameters` (category: `Validation`) for semantic issues such as negative durations, invalid burst counts, or unsupported shapes.

### 8.3 Physics setup tool

**Tool**

- `unity_create_physics_setup` → `IUnityService.CreatePhysicsSetupAsync`

**Result envelope**

- Also reuses the `ImportValidationResult`-style structure:

  ```json
  {
    "success": false,
    "path": null,
    "message": "Joint references unknown bone(s).",
    "errors": [
      {
        "category": "Validation",
        "code": "PhysicsSetup.InvalidReference",
        "message": "Joint 'SpineToHead' references unknown bone 'Head'."
      }
    ],
    "error_count": 1,
    "warning_count": 0,
    "warnings": []
  }
  ```

- Typical error codes:
  - `PhysicsSetup.InvalidJson` (category: `Validation`) when `physicsJson` cannot be parsed into `RagdollSetupContract`.
  - `PhysicsSetup.InvalidReference` (category: `Validation`) when joints or bones reference names that do not exist in the `bones` list.

These Phase 3 result shapes are designed to be **additive** and **backwards compatible** with the Phase 0/2 patterns documented above, so existing clients that rely only on `success`, `path`, and `message` can ignore the richer `errors` / `warnings` details if desired.

