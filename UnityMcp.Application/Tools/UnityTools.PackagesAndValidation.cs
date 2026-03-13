using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Validation, UPM packages, URP, and import validation.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_validate_csharp"), Description(
        "Validates C# syntax without compiling — checks balanced braces/parens, class keyword, using directives. " +
        "Returns JSON with isValid (bool) and errors (string[]). Use before saving scripts to catch issues early.")]
    public static async Task<string> ValidateCSharp(
        IUnityService unityService,
        [Description("Full C# source code to validate")]
        string code,
        CancellationToken cancellationToken = default)
    {
        return await unityService.ValidateCSharpAsync(code, cancellationToken);
    }

    [McpServerTool(Name = "unity_add_packages"), Description(
        "Adds UPM packages to Packages/manifest.json (merges with existing). " +
        "Pass a JSON object of package IDs to versions. " +
        "Example: {\"com.unity.render-pipelines.universal\":\"14.0.11\",\"com.unity.textmeshpro\":\"3.0.6\"}")]
    public static async Task<string> AddPackages(
        IUnityService unityService,
        [Description("Absolute path to the project root")]
        string projectPath,
        [Description("JSON object with package IDs and versions, e.g. {\"com.unity.render-pipelines.universal\":\"14.0.11\"}")]
        string packagesJson,
        CancellationToken cancellationToken = default)
    {
        await unityService.AddPackagesAsync(projectPath, packagesJson, cancellationToken);
        return $"Packages added to {projectPath}/Packages/manifest.json";
    }

    [McpServerTool(Name = "unity_install_packages"), Description(
        "Install UPM packages by ID. Adds each package to Packages/manifest.json in order (default version if not specified). " +
        "Returns JSON: success, installed (string[]), message.")]
    public static async Task<string> InstallPackages(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Array of UPM package IDs, e.g. com.unity.render-pipelines.universal")]
        IEnumerable<string> packages,
        CancellationToken cancellationToken = default)
    {
        var list = packages != null ? new List<string>(packages) : new List<string>();
        return await unityService.InstallPackagesAsync(projectPath, list, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_default_scene"), Description(
        "Create the default scene (Main Camera, Directional Light, Ground plane) and save Ground as prefab. " +
        "Returns JSON: success, scene_path, prefab_path, message.")]
    public static async Task<string> CreateDefaultScene(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene name without extension (e.g. MainScene)")]
        string sceneName,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateDefaultSceneAsync(projectPath, sceneName ?? "MainScene", cancellationToken);
    }

    [McpServerTool(Name = "unity_configure_urp"), Description(
        "Configure URP: Linear color space, TagManager tags and layers, default render pipeline. Returns JSON: success, message.")]
    public static async Task<string> ConfigureUrp(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        return await unityService.ConfigureUrpAsync(projectPath, cancellationToken);
    }

    [McpServerTool(Name = "unity_validate_import"), Description(
        "Full asset refresh and script compilation; report errors and warnings. Returns JSON: success, error_count, warning_count, errors, warnings, message.")]
    public static async Task<string> ValidateImport(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        return await unityService.ValidateImportAsync(projectPath, cancellationToken);
    }
}
