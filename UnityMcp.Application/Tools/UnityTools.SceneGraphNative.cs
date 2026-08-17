using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Incremental scene and prefab graph tools.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_scene_list_gameobjects"), Description("List GameObjects and basic component metadata from a Unity scene or prefab file.")]
    public static Task<string> ListSceneGameObjects(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file name/path under the project")] string fileName,
        CancellationToken cancellationToken = default)
        => unityService.ListSceneObjectsAsync(projectPath, fileName, cancellationToken);

    [McpServerTool(Name = "unity_scene_rename_gameobject"), Description("Rename a GameObject in a Unity scene or prefab by object name, hierarchy path, or fileID.")]
    public static Task<string> RenameSceneGameObject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file name/path under the project")] string fileName,
        [Description("GameObject path, name, or fileID")] string objectPath,
        [Description("New GameObject name")] string newName,
        CancellationToken cancellationToken = default)
        => unityService.RenameSceneObjectAsync(projectPath, fileName, objectPath, newName, cancellationToken);

    [McpServerTool(Name = "unity_scene_remove_gameobject"), Description("Remove a GameObject and its directly referenced components from a Unity scene or prefab.")]
    public static Task<string> RemoveSceneGameObject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file name/path under the project")] string fileName,
        [Description("GameObject path, name, or fileID")] string objectPath,
        CancellationToken cancellationToken = default)
        => unityService.RemoveSceneObjectAsync(projectPath, fileName, objectPath, cancellationToken);

    [McpServerTool(Name = "unity_diff_scenes"), Description("Compare two Unity scene or prefab files and return added, removed, and modified GameObject paths.")]
    public static Task<string> DiffScenes(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("First scene/prefab file name/path")] string fileNameA,
        [Description("Second scene/prefab file name/path")] string fileNameB,
        CancellationToken cancellationToken = default)
        => unityService.DiffSceneFilesAsync(projectPath, fileNameA, fileNameB, cancellationToken);

    [McpServerTool(Name = "unity_attach_script"), Description("Attach an existing C# script as a MonoBehaviour component reference to a GameObject.")]
    public static Task<string> AttachScript(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file name/path under the project")] string fileName,
        [Description("GameObject path, name, or fileID")] string objectPath,
        [Description("Script file name/path under the project")] string scriptFileName,
        CancellationToken cancellationToken = default)
        => unityService.AttachScriptAsync(projectPath, fileName, objectPath, scriptFileName, cancellationToken);

    [McpServerTool(Name = "unity_instantiate_prefab"), Description("Instantiate a prefab into a scene using the current file-only compatibility path.")]
    public static Task<string> InstantiatePrefab(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Target scene file name/path")] string sceneFileName,
        [Description("Prefab file name/path")] string prefabFileName,
        [Description("Instance GameObject name")] string instanceName,
        CancellationToken cancellationToken = default)
        => unityService.InstantiatePrefabAsync(projectPath, sceneFileName, prefabFileName, instanceName, cancellationToken);

    [McpServerTool(Name = "unity_save_gameobject_as_prefab"), Description("Save a GameObject from a scene as a prefab file.")]
    public static Task<string> SaveGameObjectAsPrefab(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Source scene file name/path")] string sceneFileName,
        [Description("GameObject path, name, or fileID")] string objectPath,
        [Description("Destination prefab file name/path")] string prefabFileName,
        CancellationToken cancellationToken = default)
        => unityService.SaveObjectAsPrefabAsync(projectPath, sceneFileName, objectPath, prefabFileName, cancellationToken);
}
