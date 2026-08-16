namespace UnityMcp.Core.Interfaces;

/// <summary>Validation, linting, and domain list/validate tools.</summary>
public interface IUnityValidationService
{
    Task<string> LintProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> ValidateCSharpAsync(string code, CancellationToken cancellationToken = default);
    Task<string> ValidateImportAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> CreateUiCanvasAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task<string> CreateUiLayoutAsync(string projectPath, string fileName, string layoutJson, CancellationToken cancellationToken = default);
    Task<string> ConfigureNavmeshAsync(string projectPath, string configJson, CancellationToken cancellationToken = default);
    Task<string> CreateWaypointGraphAsync(string projectPath, string fileName, string graphJson, CancellationToken cancellationToken = default);
    Task<string> CreateInputActionsAsync(string projectPath, string fileName, string actionsJson, CancellationToken cancellationToken = default);
    Task<string> CreateBasicAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default);
    Task<string> CreateAdvancedAnimatorAsync(string projectPath, string fileName, string animatorJson, CancellationToken cancellationToken = default);
    Task<string> CreateTimelineAsync(string projectPath, string fileName, string timelineJson, CancellationToken cancellationToken = default);
    Task<string> CreateVfxAssetAsync(string projectPath, string fileName, string vfxJson, CancellationToken cancellationToken = default);
    Task<string> CreatePhysicsSetupAsync(string projectPath, string fileName, string physicsJson, CancellationToken cancellationToken = default);
    Task<string> ListCamerasAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> ValidateCamerasAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> ListLightsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> ValidateLightsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> ListPhysicsObjectsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> ValidatePhysicsAsync(string projectPath, string folderName = "Assets/Scenes", CancellationToken cancellationToken = default);
    Task<string> CreateCameraAsync(string projectPath, string fileName, string parentPath, string cameraJson, CancellationToken cancellationToken = default);
    Task<string> UpdateCameraAsync(string projectPath, string fileName, string objectPath, string cameraJson, CancellationToken cancellationToken = default);
    Task<string> CreateLightAsync(string projectPath, string fileName, string parentPath, string lightJson, CancellationToken cancellationToken = default);
    Task<string> UpdateLightAsync(string projectPath, string fileName, string objectPath, string lightJson, CancellationToken cancellationToken = default);
    Task<string> CreatePhysicsBodyAsync(string projectPath, string fileName, string parentPath, string physicsJson, CancellationToken cancellationToken = default);
    Task<string> UpdatePhysicsBodyAsync(string projectPath, string fileName, string objectPath, string physicsJson, CancellationToken cancellationToken = default);
    Task<string> SearchPackagesAsync(string projectPath, string query, CancellationToken cancellationToken = default);
    Task<string> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> QueryUnityEngineDocumentationAsync(string query, string? unityVersion = null, int maxResults = 10, CancellationToken cancellationToken = default);
}
