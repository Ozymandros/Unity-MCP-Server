using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Domain-scoped camera, lighting, and physics list/validation tools.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_camera_list"), Description("List Camera components discovered in Unity scene files.")]
    public static Task<string> ListCameras(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ListCamerasAsync(projectPath, folderName, cancellationToken);

    [McpServerTool(Name = "unity_camera_validate"), Description("Validate Camera components discovered in Unity scene files.")]
    public static Task<string> ValidateCameras(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ValidateCamerasAsync(projectPath, folderName, cancellationToken);

    [McpServerTool(Name = "unity_light_list"), Description("List Light components discovered in Unity scene files.")]
    public static Task<string> ListLights(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ListLightsAsync(projectPath, folderName, cancellationToken);

    [McpServerTool(Name = "unity_light_validate"), Description("Validate Light components discovered in Unity scene files.")]
    public static Task<string> ValidateLights(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ValidateLightsAsync(projectPath, folderName, cancellationToken);

    [McpServerTool(Name = "unity_physics_list"), Description("List Rigidbody and Collider components discovered in Unity scene files.")]
    public static Task<string> ListPhysicsObjects(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ListPhysicsObjectsAsync(projectPath, folderName, cancellationToken);

    [McpServerTool(Name = "unity_physics_validate"), Description("Validate Rigidbody and Collider components discovered in Unity scene files.")]
    public static Task<string> ValidatePhysics(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene folder name/path")] string folderName = "Assets/Scenes",
        CancellationToken cancellationToken = default)
        => unityService.ValidatePhysicsAsync(projectPath, folderName, cancellationToken);
}
