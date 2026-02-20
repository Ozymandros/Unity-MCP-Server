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
    /// Checks if the specified path represents a valid Unity project.
    /// </summary>
    Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a minimal Unity Scene file (.unity) at the specified path.
    /// </summary>
    Task CreateSceneAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a C# script. If content is provided (AI-generated), writes it directly.
    /// Otherwise creates a default MonoBehaviour template.
    /// </summary>
    Task CreateScriptAsync(string relativePath, string scriptName, string? content = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a directory, optionally filtering by pattern.
    /// </summary>
    Task<IEnumerable<string>> ListAssetsAsync(string relativePath, string searchPattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the project using Unity CLI in batch mode.
    /// </summary>
    Task BuildProjectAsync(string buildTarget, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a generic text file at the specified path.
    /// </summary>
    Task CreateAssetAsync(string relativePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GameObject in an existing scene file (appends YAML).
    /// </summary>
    Task CreateGameObjectAsync(string scenePath, string gameObjectName, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Enhanced tools for AI-driven scene authoring
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a detailed scene with a list of GameObjects (camera, lights, geometry, etc.).
    /// </summary>
    Task CreateDetailedSceneAsync(string relativePath, string sceneJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a GameObject (with components and transform) to an existing scene.
    /// </summary>
    Task AddGameObjectToSceneAsync(string scenePath, string gameObjectJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Unity material file (.mat).
    /// </summary>
    Task CreateMaterialAsync(string relativePath, string materialJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Unity prefab file (.prefab).
    /// </summary>
    Task CreatePrefabAsync(string relativePath, string prefabJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the text content of a file.
    /// </summary>
    Task<string> ReadAssetAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the project.
    /// </summary>
    Task DeleteAssetAsync(string relativePath, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Project scaffolding & management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scaffolds a complete Unity project skeleton (Assets/, Scripts/, Textures/, etc.)
    /// with all necessary .meta sidecars. Idempotent — reuses existing folder.
    /// Returns the absolute path to the project root.
    /// </summary>
    Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns project info: path, name, Unity version.
    /// </summary>
    Task<string> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a folder with its .meta sidecar.
    /// </summary>
    Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default);

    // -----------------------------------------------------------------------
    // Typed asset saving (with correct .meta sidecars)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Saves a C# script with MonoImporter .meta sidecar.
    /// </summary>
    Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a text asset with DefaultImporter .meta sidecar.
    /// </summary>
    Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a texture (base64 PNG) with TextureImporter .meta sidecar.
    /// </summary>
    Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an audio clip (base64 MP3/WAV) with AudioImporter .meta sidecar.
    /// </summary>
    Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);

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
}
