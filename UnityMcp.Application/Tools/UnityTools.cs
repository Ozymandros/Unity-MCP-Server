using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>
/// Unity MCP tool definitions using the official ModelContextProtocol SDK attributes.
/// Each method is automatically discovered and registered by the SDK.
/// </summary>
[McpServerToolType]
public static class UnityTools
{
    /// <summary>
    /// Checks server connectivity.
    /// </summary>
    [McpServerTool(Name = "ping"), Description("Check server connectivity")]
    public static string Ping() => "pong";

    /// <summary>
    /// Creates a new Unity scene file (.unity).
    /// </summary>
    [McpServerTool(Name = "unity_create_scene"), Description("Creates a new Unity scene file")]
    public static async Task<string> CreateScene(
        IUnityService unityService,
        [Description("Relative path to the new scene (e.g. Assets/Scenes/NewScene.unity)")]
        string path,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateSceneAsync(path, cancellationToken);
        return $"Scene created at {path}";
    }

    /// <summary>
    /// Creates a new C# MonoBehaviour script.
    /// </summary>
    [McpServerTool(Name = "unity_create_script"), Description("Creates a new C# MonoBehaviour script")]
    public static async Task<string> CreateScript(
        IUnityService unityService,
        [Description("Relative path to the new script (e.g. Assets/Scripts/Player.cs)")]
        string path,
        [Description("Name of the class/script (e.g. Player)")]
        string scriptName,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateScriptAsync(path, scriptName, cancellationToken);
        return $"Script {scriptName} created at {path}";
    }

    /// <summary>
    /// Lists files in the Unity project Assets folder.
    /// </summary>
    [McpServerTool(Name = "unity_list_assets"), Description("Lists files in the Unity project Assets folder")]
    public static async Task<IEnumerable<string>> ListAssets(
        IUnityService unityService,
        [Description("Relative path to start listing from (default: Assets)")]
        string path = "Assets",
        [Description("Search pattern (default: *)")]
        string pattern = "*",
        CancellationToken cancellationToken = default)
    {
        return await unityService.ListAssetsAsync(path, pattern, cancellationToken);
    }

    /// <summary>
    /// Builds the Unity project for a specific target (requires Unity installed).
    /// </summary>
    [McpServerTool(Name = "unity_build_project"), Description("Builds the Unity project for a specific target (requires Unity installed)")]
    public static async Task<string> BuildProject(
        IUnityService unityService,
        [Description("Build target (Win64, OSX, Linux64)")]
        string target,
        [Description("Absolute path for the build output")]
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        await unityService.BuildProjectAsync(target, outputPath, cancellationToken);
        return $"Build initiated for target {target} to {outputPath}";
    }

    /// <summary>
    /// Creates a new GameObject in a scene.
    /// </summary>
    [McpServerTool(Name = "unity_create_gameobject"), Description("Creates a new GameObject in a scene")]
    public static async Task<string> CreateGameObject(
        IUnityService unityService,
        [Description("Relative path to the scene file")]
        string scenePath,
        [Description("Name of the new GameObject")]
        string gameObjectName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await unityService.CreateGameObjectAsync(scenePath, gameObjectName, cancellationToken);
            return $"GameObject {gameObjectName} created in {scenePath}";
        }
        catch (NotSupportedException ex)
        {
            return $"Feature not available in standalone mode: {ex.Message} Please use 'create_script' or 'create_scene'.";
        }
    }

    /// <summary>
    /// Creates a new generic asset file.
    /// </summary>
    [McpServerTool(Name = "unity_create_asset"), Description("Creates a new generic asset file")]
    public static async Task<string> CreateAsset(
        IUnityService unityService,
        [Description("Relative path to the new asset")]
        string path,
        [Description("Text content of the asset")]
        string content = "",
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateAssetAsync(path, content, cancellationToken);
        return $"Asset created at {path}";
    }
}
