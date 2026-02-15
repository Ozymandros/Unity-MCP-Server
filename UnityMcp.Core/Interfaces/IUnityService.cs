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
    /// Checks if the service can connect to a valid Unity project path.
    /// </summary>
    /// <param name="projectPath">The absolute path to the Unity project root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the path represents a valid Unity project structure.</returns>
    Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Unity Scene file (.unity) at the specified path.
    /// </summary>
    /// <param name="relativePath">Path relative to Assets folder (e.g., "Scenes/MyLevel.unity").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateSceneAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a C# script template at the specified path.
    /// </summary>
    /// <param name="relativePath">Path relative to Assets folder (e.g., "Scripts/PlayerController.cs").</param>
    /// <param name="scriptName">Name of the MonoBehaviour class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateScriptAsync(string relativePath, string scriptName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in the Assets folder, optionally filtering by extension.
    /// </summary>
    /// <param name="relativePath">The relative path to start listing from.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.unity").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of relative file paths.</returns>
    Task<IEnumerable<string>> ListAssetsAsync(string relativePath, string searchPattern = "*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the project using Unity command line interface process.
    /// </summary>
    /// <param name="buildTarget">The build target (e.g., Win64, OSXUniversal, Linux64).</param>
    /// <param name="outputPath">The absolute path where the build should be placed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BuildProjectAsync(string buildTarget, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a generic asset with the specified content.
    /// </summary>
    /// <param name="relativePath">Path relative to Assets folder.</param>
    /// <param name="content">Content of the asset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAssetAsync(string relativePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new GameObject in a scene.
    /// </summary>
    /// <param name="scenePath">Relative path to the scene file.</param>
    /// <param name="gameObjectName">Name of the GameObject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateGameObjectAsync(string scenePath, string gameObjectName, CancellationToken cancellationToken = default);
}
