using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>ScriptableObject / serialized asset tools and domain create/update.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_create_scriptable_object"), Description("Create a ScriptableObject asset by type name (Editor-backed).")]
    public static Task<string> CreateScriptableObject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Asset path, e.g. Assets/Data/Settings.asset")] string fileName,
        [Description("ScriptableObject type name")] string typeName,
        [Description("Optional properties JSON")] string? propertiesJson = null,
        CancellationToken cancellationToken = default)
        => unityService.CreateScriptableObjectAsync(projectPath, fileName, typeName, propertiesJson, cancellationToken);

    [McpServerTool(Name = "unity_read_serialized_asset"), Description("Read serialized fields from an asset (Editor-backed when available).")]
    public static Task<string> ReadSerializedAsset(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Asset file name/path")] string fileName,
        CancellationToken cancellationToken = default)
        => unityService.ReadSerializedAssetAsync(projectPath, fileName, cancellationToken);

    [McpServerTool(Name = "unity_update_serialized_asset"), Description("Update selected serialized fields on an asset (Editor-backed).")]
    public static Task<string> UpdateSerializedAsset(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Asset file name/path")] string fileName,
        [Description("JSON object of property path → value")] string propertiesJson,
        CancellationToken cancellationToken = default)
        => unityService.UpdateSerializedAssetAsync(projectPath, fileName, propertiesJson, cancellationToken);

    [McpServerTool(Name = "unity_camera_create"), Description("Create a Camera GameObject in a scene (Editor-backed).")]
    public static Task<string> CreateCamera(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Parent hierarchy path")] string parentPath,
        [Description("Camera JSON (name, fov, nearClip, farClip, depth)")] string cameraJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.CreateCameraAsync(projectPath, fileName, parentPath, cameraJson, cancellationToken);

    [McpServerTool(Name = "unity_camera_update"), Description("Update a Camera GameObject (Editor-backed).")]
    public static Task<string> UpdateCamera(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Camera object path")] string objectPath,
        [Description("Camera JSON")] string cameraJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.UpdateCameraAsync(projectPath, fileName, objectPath, cameraJson, cancellationToken);

    [McpServerTool(Name = "unity_light_create"), Description("Create a Light GameObject in a scene (Editor-backed).")]
    public static Task<string> CreateLight(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Parent hierarchy path")] string parentPath,
        [Description("Light JSON (name, type, intensity, range)")] string lightJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.CreateLightAsync(projectPath, fileName, parentPath, lightJson, cancellationToken);

    [McpServerTool(Name = "unity_light_update"), Description("Update a Light GameObject (Editor-backed).")]
    public static Task<string> UpdateLight(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Light object path")] string objectPath,
        [Description("Light JSON")] string lightJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.UpdateLightAsync(projectPath, fileName, objectPath, lightJson, cancellationToken);

    [McpServerTool(Name = "unity_physics_create_body"), Description("Create a Rigidbody + collider GameObject (Editor-backed).")]
    public static Task<string> CreatePhysicsBody(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Parent hierarchy path")] string parentPath,
        [Description("Physics JSON (name, mass, useGravity, isKinematic, boxCollider)")] string physicsJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.CreatePhysicsBodyAsync(projectPath, fileName, parentPath, physicsJson, cancellationToken);

    [McpServerTool(Name = "unity_physics_update_body"), Description("Update a Rigidbody GameObject (Editor-backed).")]
    public static Task<string> UpdatePhysicsBody(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene file")] string fileName,
        [Description("Object path")] string objectPath,
        [Description("Physics JSON")] string physicsJson = "{}",
        CancellationToken cancellationToken = default)
        => unityService.UpdatePhysicsBodyAsync(projectPath, fileName, objectPath, physicsJson, cancellationToken);

    [McpServerTool(Name = "unity_search_packages"), Description("Search UPM packages via the Unity Editor Package Manager (Editor-backed).")]
    public static Task<string> SearchPackages(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Search query")] string query,
        CancellationToken cancellationToken = default)
        => unityService.SearchPackagesAsync(projectPath, query, cancellationToken);

    [McpServerTool(Name = "unity_resolve_packages"), Description("Resolve UPM packages via the Unity Editor Package Manager (Editor-backed).")]
    public static Task<string> ResolvePackages(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        CancellationToken cancellationToken = default)
        => unityService.ResolvePackagesAsync(projectPath, cancellationToken);

    [McpServerTool(Name = "unity_query_engine_documentation"), Description("Return Unity-version-aware Scripting API and Manual search links for a query.")]
    public static Task<string> QueryUnityEngineDocumentation(
        IUnityService unityService,
        [Description("Search query")] string query,
        [Description("Unity version segment, e.g. 6000.0 or 2022.3")] string? unityVersion = null,
        [Description("Maximum results 1-25")] int maxResults = 10,
        CancellationToken cancellationToken = default)
        => unityService.QueryUnityEngineDocumentationAsync(query, unityVersion, maxResults, cancellationToken);
}
