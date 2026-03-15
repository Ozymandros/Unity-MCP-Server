using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class UnityToolsRecipeTests
{
    private IUnityService _unityService = null!;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
    }

    [Test]
    public async Task CreateCoreRecipe_WithProjectPath_CallsServiceStepsAndReturnsJson()
    {
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"installed\":[\"com.unity.render-pipelines.universal\"],\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"prefab_path\":\"Assets/Prefabs/Ground.prefab\",\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"message\":null}"));

        var result = await UnityTools.CreateCoreRecipe(_unityService, "", "", @"C:\proj", "MainScene", false);

        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("projectPath"));
        Assert.That(result, Does.Contain("proj"));
        Assert.That(result, Does.Contain("\"scene_path\":\"Assets/Scenes/MainScene.unity\""));
        Assert.That(result, Does.Contain("\"steps\""));
        await _unityService.Received(1).InstallPackagesAsync(@"C:\proj", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).ConfigureUrpAsync(@"C:\proj", Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateDefaultSceneAsync(@"C:\proj", "MainScene", Arg.Any<CancellationToken>());
        await _unityService.Received(1).ValidateImportAsync(@"C:\proj", Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCoreRecipe_WithProjectName_ScaffoldsThenRunsSteps()
    {
        _unityService.ScaffoldProjectAsync("MyGame", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"C:\output\MyGame"));
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"installed\":[],\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/Game.unity\",\"prefab_path\":\"Assets/Prefabs/Ground.prefab\",\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"message\":null}"));

        var result = await UnityTools.CreateCoreRecipe(_unityService, "MyGame", @"C:\output", "", "Game", false);

        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("MyGame").And.Contain("output"));
        Assert.That(result, Does.Contain("\"scene_path\":\"Assets/Scenes/Game.unity\""));
        await _unityService.Received(1).ScaffoldProjectAsync("MyGame", @"C:\output", null, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).InstallPackagesAsync(@"C:\output\MyGame", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCoreRecipe_NoProjectPathOrName_ReturnsFailureJson()
    {
        var result = await UnityTools.CreateCoreRecipe(_unityService, "", "", "", "MainScene", false);

        Assert.That(result, Does.Contain("\"success\":false"));
        Assert.That(result, Does.Contain("projectPath"));
        Assert.That(result, Does.Contain("projectName"));
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithExistingProject_MinimalFlags()
    {
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"installed\":[\"com.unity.render-pipelines.universal\"],\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"prefab_path\":\"Assets/Prefabs/Ground.prefab\",\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"message\":null}"));

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", null, false, false, false, false, false, false, false, false);

        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("projectPath"));
        Assert.That(result, Does.Contain("proj"));
        Assert.That(result, Does.Contain("scene_path"));
        Assert.That(result, Does.Contain("\"steps\""));
        Assert.That(result, Does.Contain("use_existing_project"));
        Assert.That(result, Does.Contain("install_packages"));
        Assert.That(result, Does.Contain("create_default_scene"));
        Assert.That(result, Does.Contain("validate_import"));
        await _unityService.Received(1).InstallPackagesAsync(@"C:\proj", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).ConfigureUrpAsync(@"C:\proj", Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateDefaultSceneAsync(@"C:\proj", "MainScene", Arg.Any<CancellationToken>());
        await _unityService.Received(1).ValidateImportAsync(@"C:\proj", Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().ConfigureNavmeshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateWaypointGraphAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateInputActionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateBasicAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateAdvancedAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateTimelineAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateVfxAssetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreatePhysicsSetupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateUiCanvasAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateUiLayoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithIncludeMainMenu_CallsUiCanvasAndLayout()
    {
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"message\":null}"));
        _unityService.CreateUiCanvasAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateUiLayoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"message\":null}"));

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", null, false, false, false, false, false, false, false, true);

        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("create_ui_canvas"));
        Assert.That(result, Does.Contain("create_ui_layout"));
        await _unityService.Received(1).CreateUiCanvasAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateUiLayoutAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithPackagesJson_CallsInstallPackagesWithCustomList()
    {
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));

        string customPackages = "[\"com.unity.inputsystem\",\"com.unity.collab-proxy\"]";
        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", customPackages, false, false, false, false, false, false, false, false);

        Assert.That(result, Does.Contain("\"success\":true"));
        await _unityService.Received(1).InstallPackagesAsync(@"C:\proj", Arg.Is<IReadOnlyList<string>>(l => l.Count == 2 && l[0] == "com.unity.inputsystem" && l[1] == "com.unity.collab-proxy"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithScaffoldAndAllFeatures()
    {
        _unityService.ScaffoldProjectAsync("PrototypeGame", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"C:\out\PrototypeGame"));
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"installed\":[],\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"message\":null}"));
        _unityService.ConfigureNavmeshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateWaypointGraphAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateInputActionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateBasicAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateAdvancedAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateTimelineAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateVfxAssetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreatePhysicsSetupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"message\":null}"));

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "PrototypeGame", @"C:\out", "", "MainScene", null, true, true, true, true, true, true, true, false);

        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("PrototypeGame"));
        Assert.That(result, Does.Contain("steps"));
        Assert.That(result, Does.Contain("scaffold"));
        Assert.That(result, Does.Contain("configure_navmesh"));
        Assert.That(result, Does.Contain("create_waypoint_graph"));
        Assert.That(result, Does.Contain("create_input_actions"));
        Assert.That(result, Does.Contain("create_basic_animator"));
        Assert.That(result, Does.Contain("create_advanced_animator"));
        Assert.That(result, Does.Contain("create_timeline"));
        Assert.That(result, Does.Contain("create_vfx_asset"));
        Assert.That(result, Does.Contain("create_physics_setup"));
        Assert.That(result, Does.Contain("validate_import"));
        await _unityService.Received(1).ScaffoldProjectAsync("PrototypeGame", @"C:\out", null, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).ConfigureNavmeshAsync(@"C:\out\PrototypeGame", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateWaypointGraphAsync(@"C:\out\PrototypeGame", "Assets/Data/PatrolRoute.waypoints.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateInputActionsAsync(@"C:\out\PrototypeGame", "Assets/Input/PlayerControls.inputactions", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateBasicAnimatorAsync(@"C:\out\PrototypeGame", "Assets/Animations/Character.animator.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateAdvancedAnimatorAsync(@"C:\out\PrototypeGame", "Assets/Animations/CharacterAdvanced.animator.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateTimelineAsync(@"C:\out\PrototypeGame", "Assets/Timelines/IntroCutscene.timeline.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreateVfxAssetAsync(@"C:\out\PrototypeGame", "Assets/VFX/ExplosionSmall.vfx.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.Received(1).CreatePhysicsSetupAsync(@"C:\out\PrototypeGame", "Assets/Physics/HumanoidRagdoll.physics.json", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithStepFailure_PropagatesFailureAndMessage()
    {
        _unityService.InstallPackagesAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.ConfigureUrpAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));
        _unityService.CreateDefaultSceneAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"message\":null}"));
        _unityService.CreateInputActionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":false,\"message\":\"Input actions validation failed.\"}"));
        _unityService.ValidateImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"message\":null}"));

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", null, false, true, false, false, false, false, false, false);

        Assert.That(result, Does.Contain("\"success\":false"));
        Assert.That(result, Does.Contain("create_input_actions"));
        Assert.That(result, Does.Contain("Input actions validation failed"));
        Assert.That(result, Does.Contain("validate_import"));
        await _unityService.Received(1).CreateInputActionsAsync(@"C:\proj", "Assets/Input/PlayerControls.inputactions", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
