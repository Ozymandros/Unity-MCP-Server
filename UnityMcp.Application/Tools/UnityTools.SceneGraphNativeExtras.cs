using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Native hierarchy and component tools (Editor-backed when available).</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_install_editor_bridge"), Description("Install the Unity MCP Editor bridge package (com.unitymcp.bridge) into the target project for live/batch native operations.")]
    public static async Task<string> InstallEditorBridge(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        await unityService.EnsureEditorBridgeInstalledAsync(projectPath, cancellationToken);
        return "{\"success\":true,\"message\":\"Editor bridge installed into Packages/com.unitymcp.bridge.\"}";
    }

    [McpServerTool(Name = "unity_scene_add_gameobject"), Description("Add a GameObject under a parent hierarchy path. Uses the Editor bridge when available; file-only mode appends at root.")]
    public static Task<string> AddSceneGameObject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("Parent hierarchy path (empty for root)")] string parentPath,
        [Description("New GameObject name")] string objectName,
        CancellationToken cancellationToken = default)
        => unityService.AddSceneGameObjectAsync(projectPath, fileName, parentPath, objectName, cancellationToken);

    [McpServerTool(Name = "unity_scene_reparent_gameobject"), Description("Reparent a GameObject in a scene or prefab (requires Editor bridge).")]
    public static Task<string> ReparentSceneGameObject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("New parent hierarchy path (empty for root)")] string newParentPath,
        CancellationToken cancellationToken = default)
        => unityService.ReparentSceneGameObjectAsync(projectPath, fileName, objectPath, newParentPath, cancellationToken);

    [McpServerTool(Name = "unity_scene_get_properties"), Description("Get transform and GameObject properties for a hierarchy path.")]
    public static Task<string> GetSceneObjectProperties(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        CancellationToken cancellationToken = default)
        => unityService.GetSceneObjectPropertiesAsync(projectPath, fileName, objectPath, cancellationToken);

    [McpServerTool(Name = "unity_scene_set_properties"), Description("Set transform/GameObject properties via SerializedObject (Editor-backed).")]
    public static Task<string> SetSceneObjectProperties(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("JSON object of properties (name, tag, layer, activeSelf, position, eulerAngles, scale)")] string propertiesJson,
        CancellationToken cancellationToken = default)
        => unityService.SetSceneObjectPropertiesAsync(projectPath, fileName, objectPath, propertiesJson, cancellationToken);

    [McpServerTool(Name = "unity_scene_set_active"), Description("Enable or disable a GameObject (Editor-backed).")]
    public static Task<string> SetSceneObjectActive(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Active state")] bool active = true,
        CancellationToken cancellationToken = default)
        => unityService.SetSceneObjectActiveAsync(projectPath, fileName, objectPath, active, cancellationToken);

    [McpServerTool(Name = "unity_component_list"), Description("List components on a GameObject.")]
    public static Task<string> ListComponents(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        CancellationToken cancellationToken = default)
        => unityService.ListComponentsAsync(projectPath, fileName, objectPath, cancellationToken);

    [McpServerTool(Name = "unity_component_add"), Description("Add a component type to a GameObject (Editor-backed).")]
    public static Task<string> AddComponent(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Component type name, e.g. Rigidbody")] string componentType,
        CancellationToken cancellationToken = default)
        => unityService.AddComponentAsync(projectPath, fileName, objectPath, componentType, cancellationToken);

    [McpServerTool(Name = "unity_component_remove"), Description("Remove a component by type and index (Editor-backed).")]
    public static Task<string> RemoveComponent(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Component type name")] string componentType,
        [Description("Index among matching components")] int componentIndex = 0,
        CancellationToken cancellationToken = default)
        => unityService.RemoveComponentAsync(projectPath, fileName, objectPath, componentType, componentIndex, cancellationToken);

    [McpServerTool(Name = "unity_component_get"), Description("Read serialized component properties (Editor-backed).")]
    public static Task<string> GetComponentProperties(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Component type name")] string componentType,
        [Description("Index among matching components")] int componentIndex = 0,
        CancellationToken cancellationToken = default)
        => unityService.GetComponentPropertiesAsync(projectPath, fileName, objectPath, componentType, componentIndex, cancellationToken);

    [McpServerTool(Name = "unity_component_set"), Description("Update serialized component properties (Editor-backed).")]
    public static Task<string> SetComponentProperties(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Component type name")] string componentType,
        [Description("JSON object of serialized property values")] string propertiesJson,
        [Description("Index among matching components")] int componentIndex = 0,
        CancellationToken cancellationToken = default)
        => unityService.SetComponentPropertiesAsync(projectPath, fileName, objectPath, componentType, propertiesJson, componentIndex, cancellationToken);

    [McpServerTool(Name = "unity_component_set_enabled"), Description("Enable or disable a Behaviour component (Editor-backed).")]
    public static Task<string> SetComponentEnabled(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Scene or prefab file")] string fileName,
        [Description("GameObject hierarchy path")] string objectPath,
        [Description("Component type name")] string componentType,
        [Description("Enabled state")] bool enabled = true,
        [Description("Index among matching components")] int componentIndex = 0,
        CancellationToken cancellationToken = default)
        => unityService.SetComponentEnabledAsync(projectPath, fileName, objectPath, componentType, enabled, componentIndex, cancellationToken);
}
