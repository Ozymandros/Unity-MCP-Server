using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>UI authoring (Canvas and layout).</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_create_ui_canvas"), Description(
        "Creates or updates a UGUI Canvas (with EventSystem) as a scene or prefab asset. " +
        "fileName can be a path or name under the project (e.g. Assets/Scenes/MainMenu.unity).")]
    public static async Task<string> CreateUiCanvas(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene or prefab file name or path under project for the UI Canvas")]
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateUiCanvasAsync(projectPath, fileName, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_ui_layout"), Description(
        "Applies a high-level UiLayout JSON description (panels, controls) to a UGUI Canvas in a scene or prefab. " +
        "See UiContracts for schema details.")]
    public static async Task<string> CreateUiLayout(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Scene or prefab file name or path under project")]
        string fileName,
        [Description("JSON UiLayout object describing panels and controls")]
        string layoutJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateUiLayoutAsync(projectPath, fileName, layoutJson, cancellationToken);
    }
}
