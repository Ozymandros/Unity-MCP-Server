using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Orchestration recipes (core and prototype).</summary>
public static partial class UnityTools
{
    // See UnityTools.cs for [McpServerToolType] and connectivity/basic tools.

    [McpServerTool(Name = "unity_create_core_recipe"), Description(
        "End-to-end recipe: scaffold project (or use existing), install URP + TextMeshPro, configure URP, " +
        "create default scene, optionally add a main menu UI, then run validation. " +
        "Returns JSON: success, projectPath, scene_path, steps[] with per-step success and message.")]
    public static async Task<string> CreateCoreRecipe(
        IUnityService unityService,
        [Description("Project name when creating a new project (ignored if projectPath is set).")]
        string projectName = "",
        [Description("Output root directory for new project (e.g. C:\\output). Ignored if projectPath is set.")]
        string outputRoot = "",
        [Description("Existing project root path. If set, scaffold is skipped and this path is used.")]
        string projectPath = "",
        [Description("Name of the default scene (without .unity). Default: MainScene")]
        string sceneName = "MainScene",
        [Description("If true, create Assets/Scenes/MainMenu.unity with Canvas and a minimal menu layout.")]
        bool includeMainMenu = false,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<object>();
        string? resolvedPath = null;
        string? scenePath = null;
        bool overallSuccess = true;

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            resolvedPath = projectPath.Trim();
            steps.Add(new { name = "use_existing_project", success = true, message = (string?)null });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return JsonSerializer.Serialize(new { success = false, projectPath = (string?)null, scene_path = (string?)null, steps = steps, message = "Either projectPath or projectName must be provided." });
            try
            {
                resolvedPath = await unityService.ScaffoldProjectAsync(projectName, string.IsNullOrWhiteSpace(outputRoot) ? null : outputRoot, null, cancellationToken);
                steps.Add(new { name = "scaffold", success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "scaffold", success = false, message = ex.Message });
                return JsonSerializer.Serialize(new { success = false, projectPath = (string?)null, scene_path = (string?)null, steps, message = ex.Message });
            }
        }

        try
        {
            string json = await unityService.InstallPackagesAsync(resolvedPath!, ["com.unity.render-pipelines.universal", "com.unity.render-pipelines.core", "com.unity.textmeshpro"], cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "install_packages", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "install_packages", success = false, message = ex.Message });
            overallSuccess = false;
        }

        try
        {
            string json = await unityService.ConfigureUrpAsync(resolvedPath!, cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "configure_urp", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "configure_urp", success = false, message = ex.Message });
            overallSuccess = false;
        }

        try
        {
            string json = await unityService.CreateDefaultSceneAsync(resolvedPath!, sceneName, cancellationToken);
            bool stepOk = ParseSuccess(json);
            scenePath = ParseScenePath(json);
            steps.Add(new { name = "create_default_scene", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "create_default_scene", success = false, message = ex.Message });
            overallSuccess = false;
        }

        if (includeMainMenu && overallSuccess)
        {
            try
            {
                string json = await unityService.CreateUiCanvasAsync(resolvedPath!, "Assets/Scenes/MainMenu.unity", cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_ui_canvas", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
                else
                {
                    const string minimalMenu = "{\"name\":\"MainMenu\",\"panels\":[{\"name\":\"RootPanel\",\"controls\":[{\"name\":\"StartButton\",\"type\":0,\"text\":\"Start Game\"}]}]}";
                    string layoutJson = await unityService.CreateUiLayoutAsync(resolvedPath!, "Assets/Scenes/MainMenu.unity", minimalMenu, cancellationToken);
                    bool layoutOk = ParseSuccess(layoutJson);
                    steps.Add(new { name = "create_ui_layout", success = layoutOk, message = layoutOk ? (string?)null : ParseMessage(layoutJson) });
                    if (!layoutOk) overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_ui_canvas", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        try
        {
            string json = await unityService.ValidateImportAsync(resolvedPath!, cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "validate_import", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "validate_import", success = false, message = ex.Message });
            overallSuccess = false;
        }

        return JsonSerializer.Serialize(new { success = overallSuccess, projectPath = resolvedPath, scene_path = scenePath, steps, message = (string?)null });
    }

    [McpServerTool(Name = "unity_create_prototype_recipe"), Description(
        "Phase 2/3 project bootstrapper: scaffold (or use existing project), URP + packages, default scene, " +
        "then optionally add main menu UI, nav config + waypoint graph, input actions, basic animator, advanced animator, timeline, VFX asset, physics setup, and validate. " +
        "Returns JSON: success, projectPath, scene_path, steps[] with per-step success and message.")]
    public static async Task<string> CreatePrototypeRecipe(
        IUnityService unityService,
        [Description("Project name when creating a new project (ignored if projectPath is set).")]
        string projectName = "",
        [Description("Output root directory for new project. Ignored if projectPath is set.")]
        string outputRoot = "",
        [Description("Existing project root path. If set, scaffold is skipped.")]
        string projectPath = "",
        [Description("Name of the default scene (without .unity). Default: MainScene")]
        string sceneName = "MainScene",
        [Description("Optional JSON array of package IDs (e.g. [\"com.unity.inputsystem\"]). When null or empty, default URP + TextMeshPro list is used.")]
        string? packagesJson = null,
        [Description("If true, add NavMesh config and a default waypoint graph (Assets/Data/PatrolRoute.waypoints.json).")]
        bool includeNav = false,
        [Description("If true, add default input actions (Assets/Input/PlayerControls.inputactions).")]
        bool includeInput = false,
        [Description("If true, add a default basic animator (Assets/Animations/Character.animator.json).")]
        bool includeAnimator = false,
        [Description("If true, add a default advanced animator (Assets/Animations/CharacterAdvanced.animator.json).")]
        bool includeAdvancedAnimator = false,
        [Description("If true, add a default timeline (Assets/Timelines/IntroCutscene.timeline.json).")]
        bool includeTimeline = false,
        [Description("If true, add a default VFX asset (Assets/VFX/ExplosionSmall.vfx.json).")]
        bool includeVfx = false,
        [Description("If true, add a default physics setup (Assets/Physics/HumanoidRagdoll.physics.json).")]
        bool includePhysics = false,
        [Description("If true, create Assets/Scenes/MainMenu.unity with Canvas and a minimal menu layout (same as core recipe).")]
        bool includeMainMenu = false,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<object>();
        string? resolvedPath = null;
        string? scenePath = null;
        bool overallSuccess = true;

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            resolvedPath = projectPath.Trim();
            steps.Add(new { name = "use_existing_project", success = true, message = (string?)null });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return JsonSerializer.Serialize(new { success = false, projectPath = (string?)null, scene_path = (string?)null, steps, message = "Either projectPath or projectName must be provided." });
            try
            {
                resolvedPath = await unityService.ScaffoldProjectAsync(projectName, string.IsNullOrWhiteSpace(outputRoot) ? null : outputRoot, null, cancellationToken);
                steps.Add(new { name = "scaffold", success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "scaffold", success = false, message = ex.Message });
                return JsonSerializer.Serialize(new { success = false, projectPath = (string?)null, scene_path = (string?)null, steps, message = ex.Message });
            }
        }

        IReadOnlyList<string> packageList = new[] { "com.unity.render-pipelines.universal", "com.unity.render-pipelines.core", "com.unity.textmeshpro" };
        if (!string.IsNullOrWhiteSpace(packagesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(packagesJson);
                if (parsed != null && parsed.Length > 0)
                    packageList = parsed;
            }
            catch { /* use default list */ }
        }
        try
        {
            string json = await unityService.InstallPackagesAsync(resolvedPath!, packageList, cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "install_packages", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "install_packages", success = false, message = ex.Message });
            overallSuccess = false;
        }

        try
        {
            string json = await unityService.ConfigureUrpAsync(resolvedPath!, cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "configure_urp", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "configure_urp", success = false, message = ex.Message });
            overallSuccess = false;
        }

        try
        {
            string json = await unityService.CreateDefaultSceneAsync(resolvedPath!, sceneName, cancellationToken);
            bool stepOk = ParseSuccess(json);
            scenePath = ParseScenePath(json);
            steps.Add(new { name = "create_default_scene", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "create_default_scene", success = false, message = ex.Message });
            overallSuccess = false;
        }

        if (includeMainMenu && overallSuccess)
        {
            try
            {
                string json = await unityService.CreateUiCanvasAsync(resolvedPath!, "Assets/Scenes/MainMenu.unity", cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_ui_canvas", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
                else
                {
                    const string minimalMenu = "{\"name\":\"MainMenu\",\"panels\":[{\"name\":\"RootPanel\",\"controls\":[{\"name\":\"StartButton\",\"type\":0,\"text\":\"Start Game\"}]}]}";
                    string layoutJson = await unityService.CreateUiLayoutAsync(resolvedPath!, "Assets/Scenes/MainMenu.unity", minimalMenu, cancellationToken);
                    bool layoutOk = ParseSuccess(layoutJson);
                    steps.Add(new { name = "create_ui_layout", success = layoutOk, message = layoutOk ? (string?)null : ParseMessage(layoutJson) });
                    if (!layoutOk) overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_ui_canvas", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeNav)
        {
            const string navConfig = "{\"agentRadius\":0.5,\"agentHeight\":2,\"agentSlope\":45,\"agentClimb\":0.4,\"cellSize\":0.1,\"cellHeight\":0.2,\"manualVoxelSize\":false}";
            try
            {
                string json = await unityService.ConfigureNavmeshAsync(resolvedPath!, navConfig, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "configure_navmesh", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "configure_navmesh", success = false, message = ex.Message });
                overallSuccess = false;
            }
            const string waypointGraph = "{\"name\":\"PatrolRoute\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}},{\"id\":\"B\",\"position\":{\"x\":5,\"y\":0,\"z\":0}}],\"edges\":[{\"from\":\"A\",\"to\":\"B\",\"bidirectional\":true}]}";
            try
            {
                string json = await unityService.CreateWaypointGraphAsync(resolvedPath!, "Assets/Data/PatrolRoute.waypoints.json", waypointGraph, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_waypoint_graph", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_waypoint_graph", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeInput)
        {
            const string inputActions = "{\"name\":\"PlayerControls\",\"maps\":[{\"name\":\"Gameplay\",\"actions\":[{\"name\":\"Move\",\"type\":\"Value\",\"expectedControlType\":\"Vector2\"},{\"name\":\"Jump\",\"type\":\"Button\",\"expectedControlType\":\"Button\"}],\"bindings\":[{\"action\":\"Move\",\"path\":\"<Keyboard>/wasd\"},{\"action\":\"Jump\",\"path\":\"<Keyboard>/space\"}]}]}";
            try
            {
                string json = await unityService.CreateInputActionsAsync(resolvedPath!, "Assets/Input/PlayerControls.inputactions", inputActions, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_input_actions", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_input_actions", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeAnimator)
        {
            const string animatorJson = "{\"name\":\"CharacterAnimator\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":null},{\"name\":\"Run\",\"clip\":null}],\"transitions\":[{\"from\":\"Idle\",\"to\":\"Run\",\"condition\":\"Speed>0.1\"}]}";
            try
            {
                string json = await unityService.CreateBasicAnimatorAsync(resolvedPath!, "Assets/Animations/Character.animator.json", animatorJson, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_basic_animator", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_basic_animator", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeAdvancedAnimator)
        {
            const string advancedAnimatorJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":null}],\"subStateMachines\":[]}]}";
            try
            {
                string json = await unityService.CreateAdvancedAnimatorAsync(resolvedPath!, "Assets/Animations/CharacterAdvanced.animator.json", advancedAnimatorJson, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_advanced_animator", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_advanced_animator", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeTimeline)
        {
            const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/IntroPose.anim\",\"start\":0.0,\"duration\":2.0}]}]}";
            try
            {
                string json = await unityService.CreateTimelineAsync(resolvedPath!, "Assets/Timelines/IntroCutscene.timeline.json", timelineJson, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_timeline", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_timeline", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includeVfx)
        {
            const string vfxJson = "{\"name\":\"ExplosionSmall\",\"duration\":1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[{\"time\":0.0,\"count\":50}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
            try
            {
                string json = await unityService.CreateVfxAssetAsync(resolvedPath!, "Assets/VFX/ExplosionSmall.vfx.json", vfxJson, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_vfx_asset", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_vfx_asset", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        if (includePhysics)
        {
            const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0},{\"name\":\"Spine\",\"colliderType\":\"Box\",\"mass\":8.0}],\"joints\":[]}";
            try
            {
                string json = await unityService.CreatePhysicsSetupAsync(resolvedPath!, "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson, cancellationToken);
                bool stepOk = ParseSuccess(json);
                steps.Add(new { name = "create_physics_setup", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
                if (!stepOk) overallSuccess = false;
            }
            catch (Exception ex)
            {
                steps.Add(new { name = "create_physics_setup", success = false, message = ex.Message });
                overallSuccess = false;
            }
        }

        try
        {
            string json = await unityService.ValidateImportAsync(resolvedPath!, cancellationToken);
            bool stepOk = ParseSuccess(json);
            steps.Add(new { name = "validate_import", success = stepOk, message = stepOk ? (string?)null : ParseMessage(json) });
            if (!stepOk) overallSuccess = false;
        }
        catch (Exception ex)
        {
            steps.Add(new { name = "validate_import", success = false, message = ex.Message });
            overallSuccess = false;
        }

        return JsonSerializer.Serialize(new { success = overallSuccess, projectPath = resolvedPath, scene_path = scenePath, steps, message = (string?)null });
    }

    private static bool ParseSuccess(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("success", out var prop))
                return prop.ValueKind == JsonValueKind.True;
        }
        catch { /* ignore */ }
        return false;
    }

    private static string? ParseMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private static string? ParseScenePath(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("scene_path", out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        catch { /* ignore */ }
        return null;
    }
}
