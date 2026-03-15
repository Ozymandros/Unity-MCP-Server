using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class UnityToolsNewTests
{
    private IUnityService _unityService = null!;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
    }

    // ---- Scaffold ----

    [Test]
    public async Task ScaffoldProject_CallsServiceAndReturnsPath()
    {
        _unityService.ScaffoldProjectAsync("MyGame", null, null, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"C:\output\MyGame"));

        var result = await UnityTools.ScaffoldProject(_unityService, "MyGame");
        Assert.That(result, Does.Contain(@"C:\output\MyGame"));
        await _unityService.Received(1).ScaffoldProjectAsync("MyGame", null, null, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ScaffoldProject_WithOutputRoot_PassesThrough()
    {
        _unityService.ScaffoldProjectAsync("Proj", @"D:\games", "2023.1.0f1", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"D:\games\Proj"));

        var result = await UnityTools.ScaffoldProject(_unityService, "Proj", @"D:\games", "2023.1.0f1");
        Assert.That(result, Does.Contain(@"D:\games\Proj"));
    }

    // ---- GetProjectInfo ----

    [Test]
    public async Task GetProjectInfo_ReturnsServiceJson()
    {
        _unityService.GetProjectInfoAsync(@"C:\proj", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"projectName\":\"proj\"}"));

        var result = await UnityTools.GetProjectInfo(_unityService, @"C:\proj");
        Assert.That(result, Does.Contain("projectName"));
    }

    // ---- CreateFolder ----

    [Test]
    public async Task CreateFolder_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateFolder(_unityService, @"C:\proj", "Assets/Custom");
        await _unityService.Received(1).CreateFolderAsync(@"C:\proj", "Assets/Custom", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Assets/Custom"));
        Assert.That(result, Does.Contain(".meta"));
    }

    // ---- SaveScript ----

    [Test]
    public async Task SaveScript_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveScript(_unityService, @"C:\proj", "Player.cs", "class Player {}");
        await _unityService.Received(1).SaveScriptAsync(@"C:\proj", "Player.cs", "class Player {}", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("saved"));
    }

    // ---- SaveText ----

    [Test]
    public async Task SaveText_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveText(_unityService, @"C:\proj", "dialogue.txt", "Hello world");
        await _unityService.Received(1).SaveTextAssetAsync(@"C:\proj", "dialogue.txt", "Hello world", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("saved"));
    }

    // ---- SaveTexture ----

    [Test]
    public async Task SaveTexture_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveTexture(_unityService, @"C:\proj", "sprite.png", "AAAA");
        await _unityService.Received(1).SaveTextureAsync(@"C:\proj", "sprite.png", "AAAA", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("saved"));
    }

    // ---- SaveAudio ----

    [Test]
    public async Task SaveAudio_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.SaveAudio(_unityService, @"C:\proj", "sfx.mp3", "BBBB");
        await _unityService.Received(1).SaveAudioAsync(@"C:\proj", "sfx.mp3", "BBBB", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("saved"));
    }

    // ---- ValidateCSharp ----

    [Test]
    public async Task ValidateCSharp_CallsServiceAndReturnsResult()
    {
        _unityService.ValidateCSharpAsync("code", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"isValid\":true}"));

        var result = await UnityTools.ValidateCSharp(_unityService, "code");
        Assert.That(result, Does.Contain("isValid"));
    }

    // ---- AddPackages ----

    [Test]
    public async Task AddPackages_CallsServiceAndReturnsMessage()
    {
        string json = "{\"com.unity.render-pipelines.universal\":\"14.0.11\"}";
        var result = await UnityTools.AddPackages(_unityService, @"C:\proj", json);
        await _unityService.Received(1).AddPackagesAsync(@"C:\proj", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("manifest.json"));
    }

    // ---- MCP-Unity contract tools ----

    [Test]
    public async Task InstallPackages_CallsServiceAndReturnsJson()
    {
        _unityService.InstallPackagesAsync(@"C:\proj", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"installed\":[\"com.unity.urp\"],\"message\":null}"));

        var result = await UnityTools.InstallPackages(_unityService, @"C:\proj", new[] { "com.unity.urp" });
        await _unityService.Received(1).InstallPackagesAsync(@"C:\proj", Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "com.unity.urp"), Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("installed"));
    }

    [Test]
    public async Task CreateDefaultScene_CallsServiceAndReturnsJson()
    {
        _unityService.CreateDefaultSceneAsync(@"C:\proj", "MainScene", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"scene_path\":\"Assets/Scenes/MainScene.unity\",\"prefab_path\":\"Assets/Prefabs/Ground.prefab\"}"));

        var result = await UnityTools.CreateDefaultScene(_unityService, @"C:\proj", "MainScene");
        await _unityService.Received(1).CreateDefaultSceneAsync(@"C:\proj", "MainScene", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("scene_path"));
        Assert.That(result, Does.Contain("prefab_path"));
    }

    [Test]
    public async Task ConfigureUrp_CallsServiceAndReturnsJson()
    {
        _unityService.ConfigureUrpAsync(@"C:\proj", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"message\":null}"));

        var result = await UnityTools.ConfigureUrp(_unityService, @"C:\proj");
        await _unityService.Received(1).ConfigureUrpAsync(@"C:\proj", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    [Test]
    public async Task ValidateImport_CallsServiceAndReturnsJson()
    {
        _unityService.ValidateImportAsync(@"C:\proj", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"error_count\":0,\"warning_count\":0,\"errors\":[],\"warnings\":[]}"));

        var result = await UnityTools.ValidateImport(_unityService, @"C:\proj");
        await _unityService.Received(1).ValidateImportAsync(@"C:\proj", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("error_count"));
    }

    // ---- UI foundations (Phase 1) ----

    [Test]
    public async Task CreateUiCanvas_CallsServiceAndReturnsJson()
    {
        _unityService.CreateUiCanvasAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":false,\"message\":\"not implemented\"}"));

        var result = await UnityTools.CreateUiCanvas(_unityService, @"C:\proj", "Assets/Scenes/MainMenu.unity");
        await _unityService.Received(1).CreateUiCanvasAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":false"));
    }

    [Test]
    public async Task CreateUiLayout_CallsServiceAndReturnsJson()
    {
        const string layoutJson = "{\"name\":\"MainMenu\",\"panels\":[]}";
        _unityService.CreateUiLayoutAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", layoutJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":false,\"message\":\"not implemented\"}"));

        var result = await UnityTools.CreateUiLayout(_unityService, @"C:\proj", "Assets/Scenes/MainMenu.unity", layoutJson);
        await _unityService.Received(1).CreateUiLayoutAsync(@"C:\proj", "Assets/Scenes/MainMenu.unity", layoutJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":false"));
    }

    // ---- Navigation (Phase 2) ----

    [Test]
    public async Task ConfigureNavmesh_CallsServiceAndReturnsJson()
    {
        const string configJson = "{\"agentRadius\":0.5,\"agentHeight\":2,\"agentSlope\":45}";
        _unityService.ConfigureNavmeshAsync(@"C:\proj", configJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Settings/NavMeshConfig.json\",\"message\":null}"));

        var result = await UnityTools.ConfigureNavmesh(_unityService, @"C:\proj", configJson);
        await _unityService.Received(1).ConfigureNavmeshAsync(@"C:\proj", configJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("NavMeshConfig"));
    }

    [Test]
    public async Task CreateWaypointGraph_CallsServiceAndReturnsJson()
    {
        const string graphJson = "{\"name\":\"Patrol\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}}],\"edges\":[]}";
        _unityService.CreateWaypointGraphAsync(@"C:\proj", "Assets/Data/Patrol.waypoints.json", graphJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Data/Patrol.waypoints.json\",\"message\":\"Waypoint graph created successfully.\"}"));

        var result = await UnityTools.CreateWaypointGraph(_unityService, @"C:\proj", "Assets/Data/Patrol.waypoints.json", graphJson);
        await _unityService.Received(1).CreateWaypointGraphAsync(@"C:\proj", "Assets/Data/Patrol.waypoints.json", graphJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    // ---- Input (Phase 2) ----

    [Test]
    public async Task CreateInputActions_CallsServiceAndReturnsJson()
    {
        const string actionsJson = "{\"name\":\"PlayerControls\",\"maps\":[{\"name\":\"Gameplay\",\"actions\":[],\"bindings\":[]}]}";
        _unityService.CreateInputActionsAsync(@"C:\proj", "Assets/Input/PlayerControls.inputactions", actionsJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Input/PlayerControls.inputactions\",\"message\":\"Input actions asset created successfully.\"}"));

        var result = await UnityTools.CreateInputActions(_unityService, @"C:\proj", "Assets/Input/PlayerControls.inputactions", actionsJson);
        await _unityService.Received(1).CreateInputActionsAsync(@"C:\proj", "Assets/Input/PlayerControls.inputactions", actionsJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    // ---- Basic Animation (Phase 2) ----

    [Test]
    public async Task CreateBasicAnimator_CallsServiceAndReturnsJson()
    {
        const string animatorJson = "{\"name\":\"CharacterAnimator\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":\"Assets/Animations/Idle.anim\"}],\"transitions\":[]}";
        _unityService.CreateBasicAnimatorAsync(@"C:\proj", "Assets/Animations/Character.animator.json", animatorJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Animations/Character.animator.json\",\"message\":\"Animator definition created successfully.\"}"));

        var result = await UnityTools.CreateBasicAnimator(_unityService, @"C:\proj", "Assets/Animations/Character.animator.json", animatorJson);
        await _unityService.Received(1).CreateBasicAnimatorAsync(@"C:\proj", "Assets/Animations/Character.animator.json", animatorJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    // ---- Advanced Animation & Timelines (Phase 3) ----

    [Test]
    public async Task CreateAdvancedAnimator_CallsServiceAndReturnsJson()
    {
        const string advancedJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"Locomotion\",\"states\":[{\"name\":\"Locomotion\",\"clip\":\"Assets/Animations/Locomotion.anim\"}],\"subStateMachines\":[]}]}";
        _unityService.CreateAdvancedAnimatorAsync(@"C:\proj", "Assets/Animations/CharacterAdvanced.animator.json", advancedJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Animations/CharacterAdvanced.animator.json\",\"message\":\"Advanced animator definition created successfully.\",\"errors\":[]}"));

        var result = await UnityTools.CreateAdvancedAnimator(_unityService, @"C:\proj", "Assets/Animations/CharacterAdvanced.animator.json", advancedJson);
        await _unityService.Received(1).CreateAdvancedAnimatorAsync(@"C:\proj", "Assets/Animations/CharacterAdvanced.animator.json", advancedJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    [Test]
    public async Task CreateTimeline_CallsServiceAndReturnsJson()
    {
        const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[]}";
        _unityService.CreateTimelineAsync(@"C:\proj", "Assets/Timelines/IntroCutscene.timeline.json", timelineJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Timelines/IntroCutscene.timeline.json\",\"message\":\"Timeline created successfully.\",\"errors\":[],\"warnings\":[]}"));

        var result = await UnityTools.CreateTimeline(_unityService, @"C:\proj", "Assets/Timelines/IntroCutscene.timeline.json", timelineJson);
        await _unityService.Received(1).CreateTimelineAsync(@"C:\proj", "Assets/Timelines/IntroCutscene.timeline.json", timelineJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    [Test]
    public async Task CreatePhysicsSetup_CallsServiceAndReturnsJson()
    {
        const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0}],\"joints\":[]}";
        _unityService.CreatePhysicsSetupAsync(@"C:\proj", "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/Physics/HumanoidRagdoll.physics.json\",\"message\":\"Physics setup created successfully.\",\"errors\":[]}"));

        var result = await UnityTools.CreatePhysicsSetup(_unityService, @"C:\proj", "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson);
        await _unityService.Received(1).CreatePhysicsSetupAsync(@"C:\proj", "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
    }

    // ---- VFX / particles (Phase 3) ----

    [Test]
    public async Task CreateVfxAsset_CallsServiceAndReturnsJson()
    {
        const string vfxJson = "{\"name\":\"ExplosionSmall\",\"duration\":1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[{\"time\":0.0,\"count\":50}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        _unityService.CreateVfxAssetAsync(@"C:\proj", "Assets/VFX/ExplosionSmall.vfx.json", vfxJson, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("{\"success\":true,\"path\":\"Assets/VFX/ExplosionSmall.vfx.json\",\"message\":\"VFX asset created successfully.\",\"errors\":[]}"));

        var result = await UnityTools.CreateVfxAsset(_unityService, @"C:\proj", "Assets/VFX/ExplosionSmall.vfx.json", vfxJson);
        await _unityService.Received(1).CreateVfxAssetAsync(@"C:\proj", "Assets/VFX/ExplosionSmall.vfx.json", vfxJson, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("\"success\":true"));
        Assert.That(result, Does.Contain("ExplosionSmall.vfx.json"));
    }
}
