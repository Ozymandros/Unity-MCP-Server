namespace UnityMcp.Core.Interfaces;

/// <summary>Project scaffolding, packages, settings, and documentation.</summary>
public interface IUnityProjectService
{
    Task<bool> IsValidProjectAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> GetServerInfoAsync(CancellationToken cancellationToken = default);
    Task<string> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task BuildProjectAsync(string projectPath, string buildTarget, string outputPath, CancellationToken cancellationToken = default);
    Task<string> ScaffoldProjectAsync(string projectName, string? outputRoot = null, string? unityVersion = null, string? template = null, CancellationToken cancellationToken = default);
    Task<string> GetProjectInfoAsync(string projectPath, CancellationToken cancellationToken = default);
    Task CreateFolderAsync(string projectPath, string folderName, CancellationToken cancellationToken = default);
    Task AddPackagesAsync(string projectPath, string packagesJson, CancellationToken cancellationToken = default);
    Task<string> ListPackagesAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> RemovePackagesAsync(string projectPath, IReadOnlyList<string> packageIds, CancellationToken cancellationToken = default);
    Task<string> VerifyPackageHealthAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> InstallPackagesAsync(string projectPath, IReadOnlyList<string> packageIds, CancellationToken cancellationToken = default);
    Task<string> ConfigureUrpAsync(string projectPath, CancellationToken cancellationToken = default);
    Task<string> ConfigureProjectSettingsAsync(string projectPath, string settingsJson, CancellationToken cancellationToken = default);
    Task<string> ConfigureBuildProfileAsync(string projectPath, string profileJson, CancellationToken cancellationToken = default);
    Task<string> QueryDocumentationAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task EnsureEditorBridgeInstalledAsync(string projectPath, CancellationToken cancellationToken = default);
}
