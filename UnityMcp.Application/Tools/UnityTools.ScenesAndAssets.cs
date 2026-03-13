using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Scene authoring, materials, prefabs, and asset read/delete.</summary>
public static partial class UnityTools
{
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
}
