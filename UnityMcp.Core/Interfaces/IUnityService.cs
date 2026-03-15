using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Service that abstracts Unity-specific operations.
/// Provides methods to manipulate Unity project files without depending on Unity Editor DLLs.
/// </summary>
public interface IUnityService
{
    /// <summary>
    /// How an agent should treat existing files when applying changes.
    /// </summary>
    public enum AgentEditMode
    {
        /// <summary>Create only; fail if file exists.</summary>
        CreateOnly = 0,
        /// <summary>Edit only; fail if file does not exist.</summary>
        EditOnly = 1,
        /// <summary>Create or edit as appropriate (auto).</summary>
        CreateOrEdit = 2,
    }
    /// <summary>
    /// Checks if the specified path represents a valid Unity project.
    /// </summary>
    Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a minimal Unity Scene file (.unity). fileName can be a path (e.g. Assets/Scenes/Main.unity) or name; no duplicate segments.
    /// </summary>
    Task CreateSceneAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a C# script. fileName can be path or name. If content is provided, writes it; otherwise default MonoBehaviour template.
    /// </summary>
    Task CreateScriptAsync(string projectPath, string fileName, string scriptName, string? content = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a directory. folderName can be path (e.g. Assets/Scripts) or name; no duplicate segments.
    /// </summary>
    Task<IEnumerable<string>> ListAssetsAsync(string projectPath, string folderName, string searchPattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the project using Unity CLI in batch mode.
    /// </summary>
    Task BuildProjectAsync(string projectPath, string buildTarget, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a generic text file. fileName can be path or name; no duplicate segments.
    /// </summary>
    Task CreateAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GameObject in an existing scene file (appends YAML). fileName = scene file path or name.
    /// </summary>
    Task CreateGameObjectAsync(string projectPath, string fileName, string gameObjectName, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Enhanced tools for AI-driven scene authoring
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a detailed scene with a list of GameObjects. fileName can be path or name; no duplicate segments.
    /// </summary>
    Task CreateDetailedSceneAsync(string projectPath, string fileName, string sceneJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a GameObject to an existing scene. fileName = scene file path or name.
    /// </summary>
    Task AddGameObjectToSceneAsync(string projectPath, string fileName, string gameObjectJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Unity material file (.mat). fileName can be path or name; no duplicate segments.
    /// </summary>
    Task CreateMaterialAsync(string projectPath, string fileName, string materialJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Unity prefab file (.prefab). fileName can be path or name; no duplicate segments.
    /// </summary>
    Task CreatePrefabAsync(string projectPath, string fileName, string prefabJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the text content of a file. fileName can be path or name.
    /// </summary>
    Task<string> ReadAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the project. fileName can be path or name.
    /// </summary>
    Task DeleteAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Project scaffolding & management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scaffolds a complete Unity project skeleton (Assets/, Scripts/, Textures/, etc.)
    /// with all necessary .meta sidecars. Idempotent — reuses existing folder.
    /// Returns the absolute path to the project root.
    /// </summary>
    Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, string? template = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns project info: path, name, Unity version.
    /// </summary>
    Task<string> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a folder with its .meta sidecar. folderName can be path or name; no duplicate segments.
    /// </summary>
    Task CreateFolderAsync(string projectPath, string folderName, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Typed asset saving (with correct .meta sidecars)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Saves a C# script with MonoImporter .meta. fileName = filename or path; no duplicate segments (e.g. Assets/Scripts once).
    /// </summary>
    Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a text asset with DefaultImporter .meta. fileName = filename or path; no duplicate segments.
    /// </summary>
    Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a texture (base64 PNG) with TextureImporter .meta. fileName = filename or path; no duplicate segments.
    /// </summary>
    Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an audio clip (base64 MP3/WAV) with AudioImporter .meta. fileName = filename or path; no duplicate segments.
    /// </summary>
    Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply a file-level change where the agent chooses whether to create or edit.
    /// Mode controls behavior when the file exists or not.
    /// Returns a JSON status payload: success, path, message.
    /// </summary>
    Task<string> ApplyFileChangeAsync(string projectPath, string fileName, string content, AgentEditMode mode = AgentEditMode.CreateOrEdit, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Validation & package management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Validates C# syntax (balanced braces/parens, class keyword present).
    /// Returns a JSON result with isValid and errors.
    /// </summary>
    Task<string> ValidateCSharpAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds UPM packages to Packages/manifest.json.
    /// </summary>
    Task AddPackagesAsync(string projectPath, string packagesJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // MCP-Unity contract tools (return JSON for client parsing)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Installs UPM packages by ID (adds to manifest with default version if needed).
    /// Returns JSON: success, installed (string[]), message.
    /// </summary>
    Task<string> InstallPackagesAsync(string projectPath, IReadOnlyList<string> packageIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the default scene (camera, directional light, ground plane) and Ground prefab.
    /// Returns JSON: success, scene_path, prefab_path, message.
    /// </summary>
    Task<string> CreateDefaultSceneAsync(string projectPath, string sceneName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures URP: Linear color space, TagManager tags/layers, default render pipeline.
    /// Returns JSON: success, message.
    /// </summary>
    Task<string> ConfigureUrpAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates import (asset refresh + compilation); reports errors and warnings.
    /// Returns JSON: success, error_count, warning_count, errors, warnings, message.
    /// </summary>
    Task<string> ValidateImportAsync(string projectPath, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // UI authoring (Phase 1 foundations)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates or updates a UGUI Canvas (with EventSystem) as a Unity scene or prefab.
    /// fileName can be a path or name; no duplicate segments.
    /// The returned JSON is currently a simple status payload and may be extended in future versions.
    /// </summary>
    Task<string> CreateUiCanvasAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a high-level UiLayout (see UiContracts) to a Canvas in a scene or prefab.
    /// fileName can be a path or name; no duplicate segments.
    /// </summary>
    Task<string> CreateUiLayoutAsync(string projectPath, string fileName, string layoutJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Navigation (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes/updates a NavMesh configuration asset from configJson (NavMeshConfig schema).
    /// Returns JSON: success, path, message, optional errors.
    /// </summary>
    Task<string> ConfigureNavmeshAsync(string projectPath, string configJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a waypoint graph asset from graphJson (WaypointGraph schema).
    /// fileName can be path or name; no duplicate segments. Returns JSON: success, path, message, optional errors.
    /// </summary>
    Task<string> CreateWaypointGraphAsync(string projectPath, string fileName, string graphJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Modern Input System (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an Input Actions asset (.inputactions) from actionsJson (InputActionsAsset schema).
    /// Returns JSON: success, path, message, optional errors.
    /// </summary>
    Task<string> CreateInputActionsAsync(string projectPath, string fileName, string actionsJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Basic Animation (Phase 2)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a basic Animator Controller asset from animatorJson (BasicAnimatorDefinition schema).
    /// Returns JSON: success, path, message, optional errors/warnings.
    /// </summary>
    Task<string> CreateBasicAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Advanced Animation & Timelines (Phase 3)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an advanced Animator definition asset (multi-layer, sub-state machines, blend trees).
    /// Returns JSON: success, path, message, errors.
    /// </summary>
    Task<string> CreateAdvancedAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Timeline definition asset from timelineJson (TimelineDefinition schema).
    /// Returns JSON: success, path, message, errors, warnings.
    /// </summary>
    Task<string> CreateTimelineAsync(string projectPath, string fileName, string timelineJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // VFX / particles (Phase 3)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a VFX particle asset from vfxJson (ParticleEffectContract schema).
    /// Returns JSON: success, path, message, errors. Validation failures use ImportValidationResult-style payloads
    /// with codes such as Vfx.InvalidJson and Vfx.InvalidParameters.
    /// </summary>
    Task<string> CreateVfxAssetAsync(string projectPath, string fileName, string vfxJson, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Advanced Physics (Phase 3)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a physics setup (ragdoll/joints) asset from physicsJson (RagdollSetupContract schema).
    /// Returns JSON: success, path, message, errors.
    /// </summary>
    Task<string> CreatePhysicsSetupAsync(string projectPath, string fileName, string physicsJson, CancellationToken cancellationToken = default);
}
