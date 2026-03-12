## Validation Pipeline and Test Strategy (Phase 1)

This document defines the extended validation pipeline behavior and required test coverage
to harden the core authoring loop (project → scene → UI → validation).

It builds on the existing `unity_validate_csharp` and `unity_validate_import` tools.

---

## 1. Validation Pipeline Overview

The validation pipeline consists of three layers:

- **Static checks** – fast, in-process checks (syntax, JSON schemas, path rules).
- **Import and compilation** – Unity-driven validation via `unity_validate_import`.
- **Scenario validation** – end-to-end recipes executed in tests and/or CI.

The intent is to:

- Fail fast on obviously invalid inputs.
- Provide structured, machine-readable errors with categories and codes.
- Keep pipeline stages composable and observable.

---

## 2. Extended unity_validate_import Behavior

`unity_validate_import` is the primary entry point for project-level validation.

### 2.1 Current behavior (Phase 0 stub)

- Implemented in `FileUnityService.ValidateImportAsync`.
- Returns a stubbed `ImportValidationResult`:

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

### 2.2 Target behavior (Phase 1 design)

When Unity CLI integration is available, `unity_validate_import` SHOULD:

1. Run Unity in batch mode against the specified project:
   - Execute an Editor script that performs:
     - `AssetDatabase.Refresh()`
     - Script compilation
2. The Editor script writes a JSON report file into the project, for example:
   - `ProjectRoot/Library/mcp_validate_result.json`
3. `FileUnityService.ValidateImportAsync` reads and returns this report to the caller.

**JSON report shape (mapped into ImportValidationResult):**

```json
{
  "success": true,
  "error_count": 0,
  "warning_count": 1,
  "errors": [],
  "warnings": [
    {
      "category": "Validation",
      "code": "Compiler.Warning",
      "message": "CS0649: Field 'Foo' is never assigned to, and will always have its default value null",
      "details": {
        "file": "Assets/Scripts/Foo.cs",
        "line": 42
      }
    }
  ],
  "message": null
}
```

Errors SHOULD be categorized as:

- `Validation` – compiler errors, missing references, broken prefabs, bad YAML.
- `ExternalTool` – Unity CLI failed to start or crashed.
- `Io` – result file missing or unreadable.

---

## 3. Validation Layers per Tool

### 3.1 Pre-save checks (before writing assets)

- **C# scripts**:
  - Use `unity_validate_csharp` to check:
    - Balanced braces/parentheses.
    - Presence of `class/struct/interface/enum`.
    - At least one `using` directive.
  - Intended to catch trivial mistakes before assets are persisted.

- **JSON-based assets** (scene, UI, nav, input, anim in later phases):
  - Validate JSON structure against contract types (e.g. `UiLayout`, nav schemas).
  - Fail with `UnityMcpErrorCategory.Validation` for:
    - Missing required properties.
    - Invalid enum values.

### 3.2 Post-generation project checks

- After generating a project or applying significant changes (new scenes, scripts, UI):
  - Optionally invoke `unity_validate_import` to ensure the project imports and compiles.

---

## 4. Test Coverage Requirements

### 4.1 Unit Tests

**Scope**

- JSON → YAML serializers (existing and future).
- `ValidateCSharpAsync`:
  - Valid code returns `isValid: true`.
  - Missing braces/parentheses, missing `class` keyword, missing `using` directive
    produce `isValid: false` with clear error messages.
- Path resolution utilities (`ResolvePath`, `ResolveAssetPath`).
- UI contract DTOs (basic serialization/deserialization round-trips).

**Examples (already present / to extend)**

- `FileUnityServiceNewToolsTests.ValidateCSharp_ValidCode_ReturnsTrue`
- `FileUnityServiceNewToolsTests.ValidateCSharp_MissingBrace_ReturnsFalse`
- `FileUnityServiceNewToolsTests.ValidateCSharp_NoClassKeyword_ReturnsFalse`

### 4.2 Integration Tests

Run against `FileUnityService` using `MockFileSystem`:

- **Scaffolding + Validation**
  - Scaffold a new project.
  - Save valid scripts and assets.
  - Call `ValidateCSharpAsync` and assert success.
  - Call `ValidateImportAsync` and assert a well-formed `ImportValidationResult` JSON:
    - Contains `success`, `error_count`, `warning_count`, `errors`, `warnings`, `message`.

- **Error Scenarios**
  - Validate clearly invalid C# and assert:
    - `isValid: false`.
    - Error messages include context (e.g. "Missing N closing brace(s)").

### 4.3 Contract Tests

For JSON-returning methods (`InstallPackagesAsync`, `CreateDefaultSceneAsync`, `ConfigureUrpAsync`, `ValidateImportAsync`):

- Store **golden JSON fixtures** for:
  - Success cases.
  - Typical error cases (e.g. missing URP asset).
- Assert:
  - Property names are stable (`scene_path`, `prefab_path`, `error_count`, `warning_count`).
  - Additional fields such as `errors` are present and correctly shaped when populated.

### 4.4 End-to-End Validation Scenarios (Design)

To be implemented when Unity CLI execution is wired in CI:

- **Scenario A – Fresh 3D URP project**
  - `unity_scaffold_project`
  - `unity_install_packages` (URP, TextMeshPro)
  - `unity_configure_urp`
  - `unity_create_default_scene`
  - `unity_validate_import`
  - Assert `success: true` and `error_count: 0`.

- **Scenario B – Simple gameplay prototype (later phases)**
  - Includes nav, input, basic anim, and UI.
  - Used as a regression guard for multi-system changes.

---

## 5. Readiness Criteria (Phase 1)

The validation pipeline is considered Phase 1–ready when:

- `ImportValidationResult` is used consistently as the result contract for `unity_validate_import`.
- Existing tests that assert JSON shapes continue to pass with the new contract types.
- All new/extended validation behaviors are:
  - Covered by unit and integration tests.
  - Documented here and in tool descriptions.

This completes the Phase 1 design of the extended validation pipeline and its associated tests,
in line with the roadmap’s emphasis on predictable, observable, and contract-governed operations.

