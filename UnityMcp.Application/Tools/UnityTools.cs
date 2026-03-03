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
/// AI agents call these tools through the MCP protocol to create Unity content.
/// </summary>
[McpServerToolType]
public static class UnityTools
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

    // -----------------------------------------------------------------------
    // Enhanced AI-driven scene authoring tools (NEW)
    // -----------------------------------------------------------------------

    [McpServerTool(Name = "unity_create_detailed_scene"), Description(
        "Creates a Unity scene with detailed GameObjects. " +
        "Pass a JSON array of GameObjects, each with name, position, scale, eulerAngles, " +
        "and components (Camera, Light, MeshFilter, MeshRenderer, BoxCollider, SphereCollider, Rigidbody, AudioSource). " +
        "fileName can be path or name; no duplicate segments.")]
    public static async Task<string> CreateDetailedScene(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene file name or path (e.g. Assets/Scenes/Level1.unity)")]
        string fileName,
        [Description("JSON array of GameObjects with name, position, scale, eulerAngles, and components")]
        string sceneJson,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateDetailedSceneAsync(projectPath, fileName, sceneJson, cancellationToken);
        return $"Detailed scene created at {fileName}";
    }

    [McpServerTool(Name = "unity_add_gameobject"), Description(
        "Appends a GameObject to an existing scene file. " +
        "Pass a JSON object with name, position, scale, eulerAngles, and an array of components. " +
        "Component types: Camera, Light, MeshFilter, MeshRenderer, BoxCollider, SphereCollider, " +
        "CapsuleCollider, Rigidbody, AudioSource. fileName = scene file name or path; no duplicate segments.")]
    public static async Task<string> AddGameObject(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene file name or path under project")]
        string fileName,
        [Description("JSON object defining the GameObject (name, position, scale, eulerAngles, components)")]
        string gameObjectJson,
        CancellationToken cancellationToken = default)
    {
        await unityService.AddGameObjectToSceneAsync(projectPath, fileName, gameObjectJson, cancellationToken);
        return $"GameObject added to {fileName}";
    }

    [McpServerTool(Name = "unity_create_material"), Description(
        "Creates a Unity material file (.mat). " +
        "Pass a JSON object with name, color {r,g,b,a}, metallic (0-1), smoothness (0-1), " +
        "emissionColor {r,g,b,a}, renderMode (0=Opaque,1=Cutout,2=Fade,3=Transparent). fileName can be path or name; no duplicate segments.")]
    public static async Task<string> CreateMaterial(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Material file name or path (e.g. Assets/Materials/RedMetal.mat)")]
        string fileName,
        [Description("JSON object with material properties (name, color, metallic, smoothness, emissionColor, renderMode)")]
        string materialJson,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateMaterialAsync(projectPath, fileName, materialJson, cancellationToken);
        return $"Material created at {fileName}";
    }

    [McpServerTool(Name = "unity_create_prefab"), Description(
        "Creates a Unity prefab file (.prefab). " +
        "Pass a JSON object with name, position, scale, eulerAngles, and components — same format as unity_add_gameobject. fileName can be path or name; no duplicate segments.")]
    public static async Task<string> CreatePrefab(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Prefab file name or path (e.g. Assets/Prefabs/Enemy.prefab)")]
        string fileName,
        [Description("JSON object defining the root GameObject (name, position, scale, components)")]
        string prefabJson,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreatePrefabAsync(projectPath, fileName, prefabJson, cancellationToken);
        return $"Prefab created at {fileName}";
    }

    [McpServerTool(Name = "unity_read_asset"), Description("Reads the text content of a file. fileName can be path or name; no duplicate segments.")]
    public static async Task<string> ReadAsset(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path under project to read")]
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await unityService.ReadAssetAsync(projectPath, fileName, cancellationToken);
    }

    [McpServerTool(Name = "unity_delete_asset"), Description("Deletes a file from the Unity project (also removes .meta sidecar). fileName can be path or name; no duplicate segments.")]
    public static async Task<string> DeleteAsset(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path under project to delete")]
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await unityService.DeleteAssetAsync(projectPath, fileName, cancellationToken);
        return $"Deleted {fileName}";
    }

    [McpServerTool(Name = "unity_create_gameobject"), Description("Creates a simple named GameObject in an existing scene (legacy — prefer unity_add_gameobject for full control). fileName = scene file name or path.")]
    public static async Task<string> CreateGameObject(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene file name or path under project")]
        string fileName,
        [Description("Name of the new GameObject")]
        string gameObjectName,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateGameObjectAsync(projectPath, fileName, gameObjectName, cancellationToken);
        return $"GameObject {gameObjectName} created in {fileName}";
    }

    // -----------------------------------------------------------------------
    // Project scaffolding & management
    // -----------------------------------------------------------------------

    [McpServerTool(Name = "unity_scaffold_project"), Description(
        "Scaffolds a complete Unity project skeleton with Assets/, Scripts/, Textures/, Audio/, " +
        "Scenes/, Prefabs/, Materials/, Text/, ProjectSettings/, and Packages/ — all with .meta sidecars. " +
        "Idempotent: reuses the project folder if it already exists. Returns the absolute path.")]
    public static async Task<string> ScaffoldProject(
        IUnityService unityService,
        [Description("Project folder name (e.g. MyGame). Used as-is — no timestamps added.")]
        string projectName,
        [Description("Parent directory for the project (default: ./output)")]
        string outputRoot = "",
        [Description("Unity version for ProjectVersion.txt (default: 2022.3.0f1)")]
        string unityVersion = "",
        CancellationToken cancellationToken = default)
    {
        string path = await unityService.ScaffoldProjectAsync(
            projectName,
            string.IsNullOrEmpty(outputRoot) ? null : outputRoot,
            string.IsNullOrEmpty(unityVersion) ? null : unityVersion,
            cancellationToken);
        return $"Project scaffolded at {path}";
    }

    [McpServerTool(Name = "unity_get_project_info"), Description(
        "Returns project metadata (name, absolute path, Unity version, whether Assets/ exists) as JSON.")]
    public static async Task<string> GetProjectInfo(
        IUnityService unityService,
        [Description("Absolute path to the Unity project root")]
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        return await unityService.GetProjectInfoAsync(projectPath, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_folder"), Description(
        "Creates a folder inside the Unity project with a .meta sidecar. folderName can be path or name; no duplicate segments.")]
    public static async Task<string> CreateFolder(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Folder name or path under project to create")]
        string folderName,
        CancellationToken cancellationToken = default)
    {
        await unityService.CreateFolderAsync(projectPath, folderName, cancellationToken);
        return $"Folder created at {folderName} (with .meta)";
    }

    // -----------------------------------------------------------------------
    // Typed asset saving tools
    // -----------------------------------------------------------------------

    [McpServerTool(Name = "unity_save_script"), Description(
        "Saves AI-generated C# code with MonoImporter .meta. fileName = filename (e.g. Player.cs) or path; no duplicate segments (Assets/Scripts once).")]
    public static async Task<string> SaveScript(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path (e.g. Player.cs, Assets/Scripts/Player.cs)")]
        string fileName,
        [Description("Full C# source code")]
        string content,
        CancellationToken cancellationToken = default)
    {
        await unityService.SaveScriptAsync(projectPath, fileName, content, cancellationToken);
        return "Script saved (with .meta)";
    }

    [McpServerTool(Name = "unity_save_text"), Description(
        "Saves a text asset with DefaultImporter .meta. fileName = filename or path; no duplicate segments.")]
    public static async Task<string> SaveText(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path (e.g. dialogue.txt)")]
        string fileName,
        [Description("Text content to save")]
        string content,
        CancellationToken cancellationToken = default)
    {
        await unityService.SaveTextAssetAsync(projectPath, fileName, content, cancellationToken);
        return "Text asset saved (with .meta)";
    }

    [McpServerTool(Name = "unity_save_texture"), Description(
        "Saves a base64-encoded PNG/JPG with TextureImporter .meta. fileName = filename or path; no duplicate segments.")]
    public static async Task<string> SaveTexture(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path (e.g. sprite.png)")]
        string fileName,
        [Description("Base64-encoded image data (PNG or JPG)")]
        string base64Data,
        CancellationToken cancellationToken = default)
    {
        await unityService.SaveTextureAsync(projectPath, fileName, base64Data, cancellationToken);
        return "Texture saved (with .meta)";
    }

    [McpServerTool(Name = "unity_save_audio"), Description(
        "Saves a base64-encoded audio file with AudioImporter .meta. fileName = filename or path; no duplicate segments.")]
    public static async Task<string> SaveAudio(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("File name or path (e.g. explosion.mp3)")]
        string fileName,
        [Description("Base64-encoded audio data (MP3 or WAV)")]
        string base64Data,
        CancellationToken cancellationToken = default)
    {
        await unityService.SaveAudioAsync(projectPath, fileName, base64Data, cancellationToken);
        return "Audio saved (with .meta)";
    }

    // -----------------------------------------------------------------------
    // Validation & package management
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    // MCP-Unity contract tools (return JSON for client parsing)
    // -----------------------------------------------------------------------

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

