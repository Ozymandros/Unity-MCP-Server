using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>UPM, ProjectSettings, and build-profile management tools.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_list_packages"), Description("List installed UPM package dependencies from Packages/manifest.json.")]
    public static Task<string> ListPackages(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        CancellationToken cancellationToken = default)
        => unityService.ListPackagesAsync(projectPath, cancellationToken);

    [McpServerTool(Name = "unity_remove_packages"), Description("Remove UPM package IDs from Packages/manifest.json.")]
    public static Task<string> RemovePackages(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Package IDs to remove")] IEnumerable<string> packages,
        CancellationToken cancellationToken = default)
        => unityService.RemovePackagesAsync(projectPath, packages?.ToArray() ?? Array.Empty<string>(), cancellationToken);

    [McpServerTool(Name = "unity_verify_package_health"), Description("Validate manifest and package-lock health for installed UPM packages.")]
    public static Task<string> VerifyPackageHealth(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        CancellationToken cancellationToken = default)
        => unityService.VerifyPackageHealthAsync(projectPath, cancellationToken);

    [McpServerTool(Name = "unity_configure_project_settings"), Description("Write safe ProjectSettings sidecar data and patch selected settings such as tags and layers.")]
    public static Task<string> ConfigureProjectSettings(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Project settings JSON")] string settingsJson,
        CancellationToken cancellationToken = default)
        => unityService.ConfigureProjectSettingsAsync(projectPath, settingsJson, cancellationToken);

    [McpServerTool(Name = "unity_configure_build_profile"), Description("Write a build profile JSON asset under Assets/Settings/BuildProfiles.")]
    public static Task<string> ConfigureBuildProfile(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Build profile JSON")] string profileJson,
        CancellationToken cancellationToken = default)
        => unityService.ConfigureBuildProfileAsync(projectPath, profileJson, cancellationToken);

    [McpServerTool(Name = "unity_query_documentation"), Description("Search local Unity MCP README, Docs, and Skills markdown for relevant guidance.")]
    public static Task<string> QueryDocumentation(
        IUnityService unityService,
        [Description("Search query")] string query,
        [Description("Maximum number of results, 1-25")] int maxResults = 10,
        CancellationToken cancellationToken = default)
        => unityService.QueryDocumentationAsync(query, maxResults, cancellationToken);
}
