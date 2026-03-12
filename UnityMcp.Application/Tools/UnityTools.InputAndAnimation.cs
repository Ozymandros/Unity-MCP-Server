using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Input System and animation (basic animator, advanced animator, timeline).</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_create_input_actions"), Description(
        "Creates an Input Actions asset (.inputactions) from JSON (name, maps with actions and bindings). " +
        "See InputContracts. Returns JSON: success, path, message, errors.")]
    public static async Task<string> CreateInputActions(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Asset path or name, e.g. Assets/Input/PlayerControls.inputactions")]
        string fileName,
        [Description("Input actions JSON (InputActionsAsset schema)")]
        string actionsJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateInputActionsAsync(projectPath, fileName, actionsJson, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_basic_animator"), Description(
        "Creates a basic Animator Controller definition (JSON surrogate). Validates referenced clip paths. " +
        "See AnimationContracts. Returns JSON: success, path, message, errors, warnings.")]
    public static async Task<string> CreateBasicAnimator(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Animator asset path, e.g. Assets/Animations/CharacterAnimator.animator.json")]
        string fileName,
        [Description("Animator definition JSON (name, defaultState, states, transitions)")]
        string animatorJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateBasicAnimatorAsync(projectPath, fileName, animatorJson, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_advanced_animator"), Description(
        "Creates an advanced Animator definition (multi-layer, sub-state machines, blend trees) as a JSON surrogate. " +
        "See advanced Phase 3 animation contracts. Returns JSON: success, path, message, errors.")]
    public static async Task<string> CreateAdvancedAnimator(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Advanced animator asset path, e.g. Assets/Animations/CharacterAdvanced.animator.json")]
        string fileName,
        [Description("Advanced animator JSON (layers, state machines, blend trees)")]
        string animatorJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateAdvancedAnimatorAsync(projectPath, fileName, animatorJson, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_timeline"), Description(
        "Creates a Timeline definition asset from JSON (tracks and clips). " +
        "Returns JSON: success, path, message, errors, warnings.")]
    public static async Task<string> CreateTimeline(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Timeline asset path, e.g. Assets/Timelines/IntroCutscene.timeline.json")]
        string fileName,
        [Description("Timeline JSON (TimelineDefinition schema)")]
        string timelineJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateTimelineAsync(projectPath, fileName, timelineJson, cancellationToken);
    }
}
