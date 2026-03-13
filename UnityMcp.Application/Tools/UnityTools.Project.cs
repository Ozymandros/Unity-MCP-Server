using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Project scaffolding, folders, and typed asset saving.</summary>
public static partial class UnityTools
{
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
}
