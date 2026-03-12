using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>
/// Unity MCP tool definitions using the official ModelContextProtocol SDK attributes.
/// Each method is automatically discovered and registered by the SDK.
/// AI agents call these tools through the MCP protocol to create Unity content.
/// </summary>
[McpServerToolType]
public static partial class UnityTools
{
    // -----------------------------------------------------------------------
    // Connectivity
    // -----------------------------------------------------------------------

    [McpServerTool(Name = "ping"), Description("Check server connectivity")]
    public static string Ping() => "pong";

    // -----------------------------------------------------------------------
    // Basic file tools (existing)
    // -----------------------------------------------------------------------

    [McpServerTool(Name = "unity_create_scene"), Description("Creates a new Unity scene file with default camera and light. fileName can be path (e.g. Assets/Scenes/NewScene.unity) or name; no duplicate segments.")]
    public static async Task<string> CreateScene(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene file name or path under project (e.g. Assets/Scenes/NewScene.unity)")]
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateSceneAsync(projectPath, fileName, cancellationToken);
        return $"Scene created at {fileName}";
    }

    [McpServerTool(Name = "unity_create_script"), Description(
        "Creates a C# script file. If content is provided, writes the full AI-generated code. " +
        "Otherwise creates a default MonoBehaviour template. fileName can be path or name; no duplicate segments.")]
    public static async Task<string> CreateScript(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Script file name or path under project (e.g. Assets/Scripts/Player.cs)")]
        string fileName,
        [Description("Name of the class (e.g. Player)")]
        string scriptName,
        [Description("Full C# script content. If omitted, a default MonoBehaviour template is used.")]
        string content = "",
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateScriptAsync(projectPath, fileName, scriptName, string.IsNullOrEmpty(content) ? null : content, cancellationToken);
        return $"Script {scriptName} created at {fileName}";
    }

    [McpServerTool(Name = "unity_list_assets"), Description("Lists files in the Unity project directory. folderName can be path (e.g. Assets/Scripts) or name; no duplicate segments.")]
    public static async Task<IEnumerable<string>> ListAssets(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Folder name or path to list (default: Assets)")]
        string folderName = "Assets",
        [Description("Search pattern, e.g. *.cs, *.unity (default: *)")]
        string pattern = "*",
        CancellationToken cancellationToken = default)
    {
        return await unityService.ListAssetsAsync(projectPath, folderName, pattern, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_asset"), Description("Creates a generic text asset file. fileName can be path or name; no duplicate segments.")]
    public static async Task<string> CreateAsset(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Asset file name or path under project")]
        string fileName,
        [Description("Text content of the asset")]
        string content = "",
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateAssetAsync(projectPath, fileName, content, cancellationToken);
        return $"Asset created at {fileName}";
    }

    [McpServerTool(Name = "unity_build_project"), Description("Builds the Unity project for a specific target (requires Unity installed)")]
    public static async Task<string> BuildProject(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Build target (Win64, OSX, Linux64)")]
        string target,
        [Description("Absolute path for the build output")]
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        await unityService.BuildProjectAsync(projectPath, target, outputPath, cancellationToken);
        return $"Build completed for target {target} at {outputPath}";
    }

    // Scene authoring, project, packages, UI, navigation, input/animation, VFX/physics: see partials (UnityTools.*.cs).
    // Recipes: UnityTools.Recipes.cs.
}
