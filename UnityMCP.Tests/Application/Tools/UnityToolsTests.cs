using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;
using UnityMcp.Infrastructure.Unity;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class UnityToolsTests
{
    private IUnityService _unityService = null!;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
    }

    // -----------------------------------------------------------------------
    // Ping
    // -----------------------------------------------------------------------

    [Test]
    public void Ping_ReturnsPong()
    {
        Assert.That(UnityTools.Ping(), Is.EqualTo("pong"));
    }

    // -----------------------------------------------------------------------
    // CreateScene
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateScene_ValidPath_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScene(_unityService, @"C:\proj", "Scenes/TestScene.unity");
        await _unityService.Received(1).CreateSceneAsync(@"C:\proj", "Scenes/TestScene.unity", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/TestScene.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateScript
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateScript_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScript(_unityService, @"C:\proj", "Scripts/Player.cs", "Player");
        await _unityService.Received(1).CreateScriptAsync(@"C:\proj", "Scripts/Player.cs", "Player", null, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    [Test]
    public async Task CreateScript_WithAIContent_PassesContentToService()
    {
        string aiCode = "using UnityEngine;\npublic class Player : MonoBehaviour { void Update() { } }";
        var result = await UnityTools.CreateScript(_unityService, @"C:\proj", "Scripts/Player.cs", "Player", aiCode);
        await _unityService.Received(1).CreateScriptAsync(@"C:\proj", "Scripts/Player.cs", "Player", aiCode, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    // -----------------------------------------------------------------------
    // ListAssets
    // -----------------------------------------------------------------------

    [Test]
    public async Task ListAssets_ValidRequest_ReturnsAssetsList()
    {
        var expected = new List<string> { "Assets/Textures/Player.png", "Assets/Textures/Ground.png" };
        _unityService.ListAssetsAsync(@"C:\proj", "Assets/Textures", "*.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<string>)expected));

        var result = await UnityTools.ListAssets(_unityService, @"C:\proj", "Assets/Textures", "*.png");
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task ListAssets_NoMatches_ReturnsEmptyList()
    {
        _unityService.ListAssetsAsync(@"C:\proj", "Assets/Audio", "*.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Enumerable.Empty<string>()));
        var result = await UnityTools.ListAssets(_unityService, @"C:\proj", "Assets/Audio", "*.wav");
        Assert.That(result, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // BuildProject
    // -----------------------------------------------------------------------

    [Test]
    public async Task BuildProject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.BuildProject(_unityService, @"C:\proj", "Win64", "C:/Builds/Output");
        await _unityService.Received(1).BuildProjectAsync(@"C:\proj", "Win64", "C:/Builds/Output", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Win64"));
    }

    // -----------------------------------------------------------------------
    // CreateAsset
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateAsset_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateAsset(_unityService, @"C:\proj", "Materials/Test.mat", "content");
        await _unityService.Received(1).CreateAssetAsync(@"C:\proj", "Materials/Test.mat", "content", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Test.mat"));
    }

    [Test]
    public async Task CreateAsset_EmptyContent_DefaultsToEmptyString()
    {
        var result = await UnityTools.CreateAsset(_unityService, @"C:\proj", "Materials/Empty.mat");
        await _unityService.Received(1).CreateAssetAsync(@"C:\proj", "Materials/Empty.mat", "", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Empty.mat"));
    }

    // -----------------------------------------------------------------------
    // CreateGameObject (legacy)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateGameObject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateGameObject(_unityService, @"C:\proj", "Scenes/Main.unity", "Cube");
        await _unityService.Received(1).CreateGameObjectAsync(@"C:\proj", "Scenes/Main.unity", "Cube", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Cube"));
    }

    // -----------------------------------------------------------------------
    // CreateDetailedScene (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateDetailedScene_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """[{"name":"Camera","tag":"MainCamera","position":{"x":0,"y":1,"z":-10},"components":[{"type":"Camera"}]}]""";
        var result = await UnityTools.CreateDetailedScene(_unityService, @"C:\proj", "Assets/Scenes/Level1.unity", json);
        await _unityService.Received(1).CreateDetailedSceneAsync(@"C:\proj", "Assets/Scenes/Level1.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Level1.unity"));
    }

    // -----------------------------------------------------------------------
    // AddGameObject (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task AddGameObject_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Sphere","position":{"x":3,"y":0,"z":0},"components":[{"type":"MeshFilter","mesh":"Sphere"},{"type":"MeshRenderer"}]}""";
        var result = await UnityTools.AddGameObject(_unityService, @"C:\proj", "Scenes/Main.unity", json);
        await _unityService.Received(1).AddGameObjectToSceneAsync(@"C:\proj", "Scenes/Main.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/Main.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateMaterial (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateMaterial_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"RedMetal","color":{"r":1,"g":0,"b":0,"a":1},"metallic":0.8,"smoothness":0.6}""";
        var result = await UnityTools.CreateMaterial(_unityService, @"C:\proj", "Assets/Materials/RedMetal.mat", json);
        await _unityService.Received(1).CreateMaterialAsync(@"C:\proj", "Assets/Materials/RedMetal.mat", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("RedMetal.mat"));
    }

    // -----------------------------------------------------------------------
    // CreatePrefab (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreatePrefab_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Enemy","components":[{"type":"MeshFilter","mesh":"Capsule"},{"type":"Rigidbody","mass":2}]}""";
        var result = await UnityTools.CreatePrefab(_unityService, @"C:\proj", "Assets/Prefabs/Enemy.prefab", json);
        await _unityService.Received(1).CreatePrefabAsync(@"C:\proj", "Assets/Prefabs/Enemy.prefab", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Enemy.prefab"));
    }

    // -----------------------------------------------------------------------
    // ReadAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task ReadAsset_ReturnsFileContent()
    {
        _unityService.ReadAssetAsync(@"C:\proj", "Assets/Scripts/Player.cs", Arg.Any<CancellationToken>())
            .Returns("using UnityEngine; class Player {}");

        var result = await UnityTools.ReadAsset(_unityService, @"C:\proj", "Assets/Scripts/Player.cs");
        Assert.That(result, Does.Contain("using UnityEngine"));
    }

    // -----------------------------------------------------------------------
    // DeleteAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task DeleteAsset_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.DeleteAsset(_unityService, @"C:\proj", "Assets/Old/Legacy.cs");
        await _unityService.Received(1).DeleteAssetAsync(@"C:\proj", "Assets/Old/Legacy.cs", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Deleted"));
    }
}

// ============================================================================
// Unity YAML Writer Tests (infrastructure)
// ============================================================================

[TestFixture]
public class UnityYamlWriterTests
{
    [SetUp]
    public void SetUp()
    {
        UnityYamlWriter.ResetFileIdCounter();
    }

    [Test]
    public void Header_ContainsYamlTag()
    {
        var header = UnityYamlWriter.Header();
        Assert.That(header, Does.Contain("%YAML 1.1"));
        Assert.That(header, Does.Contain("tag:unity3d.com,2011:"));
    }

    [Test]
    public void WriteScene_WithCameraAndLight_ContainsExpectedElements()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Main Camera", Tag = "MainCamera", Position = new Vector3Def(0, 1, -10),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Camera)] },
            new() { Name = "Directional Light", EulerAngles = new Vector3Def(50, -30, 0),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Light)] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("OcclusionCullingSettings"));
        Assert.That(yaml, Does.Contain("RenderSettings"));
        Assert.That(yaml, Does.Contain("m_Name: Main Camera"));
        Assert.That(yaml, Does.Contain("m_TagString: MainCamera"));
        Assert.That(yaml, Does.Contain("m_Name: Directional Light"));
        Assert.That(yaml, Does.Contain("Camera:"));
        Assert.That(yaml, Does.Contain("Light:"));
    }

    [Test]
    public void WriteScene_WithMeshObject_ContainsMeshFilterAndRenderer()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Cube", Position = new Vector3Def(0, 0.5f, 0),
                     Components = [
                         new ComponentDef(UnityYamlWriter.ClassId_MeshFilter)
                         { Properties = { ["mesh"] = "Cube" } },
                         new ComponentDef(UnityYamlWriter.ClassId_MeshRenderer),
                         new ComponentDef(UnityYamlWriter.ClassId_BoxCollider),
                     ] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("MeshFilter:"));
        Assert.That(yaml, Does.Contain("MeshRenderer:"));
        Assert.That(yaml, Does.Contain("BoxCollider:"));
        Assert.That(yaml, Does.Contain("m_Name: Cube"));
    }

    [Test]
    public void WritePrefab_ContainsGameObjectAndTransform()
    {
        var go = new GameObjectDef
        {
            Name = "Enemy",
            Scale = new Vector3Def(2, 2, 2),
            Components = [
                new ComponentDef(UnityYamlWriter.ClassId_Rigidbody)
                { Properties = { ["mass"] = 5f, ["useGravity"] = true } },
            ]
        };

        var yaml = UnityYamlWriter.WritePrefab(go);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: Enemy"));
        Assert.That(yaml, Does.Contain("m_LocalScale: {x: 2, y: 2, z: 2}"));
        Assert.That(yaml, Does.Contain("Rigidbody:"));
        Assert.That(yaml, Does.Contain("m_Mass: 5"));
        Assert.That(yaml, Does.Contain("m_UseGravity: 1"));
    }

    [Test]
    public void WriteMaterial_ContainsShaderAndProperties()
    {
        var mat = new MaterialDef
        {
            Name = "BluePlastic",
            Color = new ColorDef(0, 0.3f, 0.8f, 1),
            Metallic = 0.1f,
            Smoothness = 0.7f,
        };

        var yaml = UnityYamlWriter.WriteMaterial(mat);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: BluePlastic"));
        Assert.That(yaml, Does.Contain("_Color:"));
        Assert.That(yaml, Does.Contain("_Metallic: 0.1"));
        Assert.That(yaml, Does.Contain("_Smoothness: 0.7"));
    }

    [Test]
    public void WriteGameObjectFragment_NoHeader()
    {
        var go = new GameObjectDef { Name = "TestGO" };
        var fragment = UnityYamlWriter.WriteGameObjectFragment(go);

        Assert.That(fragment, Does.Not.Contain("%YAML"));
        Assert.That(fragment, Does.Contain("m_Name: TestGO"));
    }

    [Test]
    public void FileIdCounter_IsSequential()
    {
        var id1 = UnityYamlWriter.NextFileId();
        var id2 = UnityYamlWriter.NextFileId();
        Assert.That(id2, Is.EqualTo(id1 + 1));
    }
}

// -----------------------------------------------------------------------
// New tools tests (scaffold, typed saves, validation, packages)
// -----------------------------------------------------------------------

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
        _unityService.ScaffoldProjectAsync("MyGame", null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(@"C:\output\MyGame"));

        var result = await UnityTools.ScaffoldProject(_unityService, "MyGame");
        Assert.That(result, Does.Contain(@"C:\output\MyGame"));
        await _unityService.Received(1).ScaffoldProjectAsync("MyGame", null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ScaffoldProject_WithOutputRoot_PassesThrough()
    {
        _unityService.ScaffoldProjectAsync("Proj", @"D:\games", "2023.1.0f1", Arg.Any<CancellationToken>())
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

    // ---- Core recipe (Phase 1 orchestration) ----

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
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCoreRecipe_WithProjectName_ScaffoldsThenRunsSteps()
    {
        _unityService.ScaffoldProjectAsync("MyGame", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        await _unityService.Received(1).ScaffoldProjectAsync("MyGame", @"C:\output", null, Arg.Any<CancellationToken>());
        await _unityService.Received(1).InstallPackagesAsync(@"C:\output\MyGame", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateCoreRecipe_NoProjectPathOrName_ReturnsFailureJson()
    {
        var result = await UnityTools.CreateCoreRecipe(_unityService, "", "", "", "MainScene", false);

        Assert.That(result, Does.Contain("\"success\":false"));
        Assert.That(result, Does.Contain("projectPath"));
        Assert.That(result, Does.Contain("projectName"));
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ---- Prototype recipe (Phase 2/3 orchestration) ----

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

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", false, false, false, false, false, false, false);

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
        await _unityService.DidNotReceive().ScaffoldProjectAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().ConfigureNavmeshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateWaypointGraphAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateInputActionsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateBasicAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateAdvancedAnimatorAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateTimelineAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreateVfxAssetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unityService.DidNotReceive().CreatePhysicsSetupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePrototypeRecipe_WithScaffoldAndAllFeatures()
    {
        _unityService.ScaffoldProjectAsync("PrototypeGame", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "PrototypeGame", @"C:\out", "", "MainScene", true, true, true, true, true, true, true);

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
        await _unityService.Received(1).ScaffoldProjectAsync("PrototypeGame", @"C:\out", null, Arg.Any<CancellationToken>());
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

        var result = await UnityTools.CreatePrototypeRecipe(_unityService, "", "", @"C:\proj", "MainScene", false, true, false, false, false, false, false);

        Assert.That(result, Does.Contain("\"success\":false"));
        Assert.That(result, Does.Contain("create_input_actions"));
        Assert.That(result, Does.Contain("Input actions validation failed"));
        Assert.That(result, Does.Contain("validate_import"));
        await _unityService.Received(1).CreateInputActionsAsync(@"C:\proj", "Assets/Input/PlayerControls.inputactions", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

// -----------------------------------------------------------------------
// MetaFileWriter + FileUnityService integration tests
// -----------------------------------------------------------------------

[TestFixture]
public class MetaFileWriterTests
{
    private MockFileSystem _mockFs = null!;
    private MetaFileWriter _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFs = new MockFileSystem();
        _mockFs.Directory.CreateDirectory(@"C:\project");
        _writer = new MetaFileWriter(_mockFs);
    }

    [Test]
    public async Task WriteScriptMeta_CreatesValidMetaFile()
    {
        string scriptPath = @"C:\project\Test.cs";
        _mockFs.File.WriteAllText(scriptPath, "class Test {}");
        await _writer.WriteScriptMetaAsync(scriptPath);

        string metaPath = scriptPath + ".meta";
        Assert.That(_mockFs.File.Exists(metaPath), Is.True);
        string content = _mockFs.File.ReadAllText(metaPath);
        Assert.That(content, Does.Contain("MonoImporter:"));
        Assert.That(content, Does.Contain("fileFormatVersion: 2"));
        Assert.That(content, Does.Contain("guid:"));
    }

    [Test]
    public async Task WriteDefaultMeta_CreatesValidMetaFile()
    {
        string txtPath = @"C:\project\readme.txt";
        _mockFs.File.WriteAllText(txtPath, "hello");
        await _writer.WriteDefaultMetaAsync(txtPath);

        string content = _mockFs.File.ReadAllText(txtPath + ".meta");
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public async Task WriteTextureMeta_ContainsTextureImporter()
    {
        string imgPath = @"C:\project\img.png";
        _mockFs.File.WriteAllBytes(imgPath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _writer.WriteTextureMetaAsync(imgPath);

        string content = _mockFs.File.ReadAllText(imgPath + ".meta");
        Assert.That(content, Does.Contain("TextureImporter:"));
        Assert.That(content, Does.Contain("maxTextureSize: 2048"));
    }

    [Test]
    public async Task WriteAudioMeta_ContainsAudioImporter()
    {
        string audioPath = @"C:\project\clip.mp3";
        _mockFs.File.WriteAllBytes(audioPath, new byte[] { 0xFF, 0xFB });
        await _writer.WriteAudioMetaAsync(audioPath);

        string content = _mockFs.File.ReadAllText(audioPath + ".meta");
        Assert.That(content, Does.Contain("AudioImporter:"));
        Assert.That(content, Does.Contain("sampleRateOverride: 44100"));
    }

    [Test]
    public async Task WriteFolderMeta_ContainsFolderAsset()
    {
        string folder = @"C:\project\MyFolder";
        _mockFs.Directory.CreateDirectory(folder);
        await _writer.WriteFolderMetaAsync(folder);

        string content = _mockFs.File.ReadAllText(folder + ".meta");
        Assert.That(content, Does.Contain("folderAsset: yes"));
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public void NewGuid_Returns32HexChars()
    {
        string guid = MetaFileWriter.NewGuid();
        Assert.That(guid.Length, Is.EqualTo(32));
        Assert.That(guid, Does.Match("^[0-9a-f]{32}$"));
    }
}

// -----------------------------------------------------------------------
// FileUnityService integration tests (new tools)
// -----------------------------------------------------------------------

[TestFixture]
public class FileUnityServiceNewToolsTests
{
    private MockFileSystem _mockFs = null!;
    private FileUnityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFs = new MockFileSystem();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        _service = new FileUnityService(logger, processRunner, _mockFs);
    }

    [Test]
    public async Task ScaffoldProject_CreatesAllFolders()
    {
        string projectPath = await _service.ScaffoldProjectAsync("TestProject", @"C:\output");

        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Scripts")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Textures")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Audio")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Text")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "ProjectSettings")), Is.True);
        Assert.That(_mockFs.Directory.Exists(_mockFs.Path.Combine(projectPath, "Packages")), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesMetaSidecars()
    {
        string projectPath = await _service.ScaffoldProjectAsync("MetaTest", @"C:\output");

        // Assets folder should have a .meta
        Assert.That(_mockFs.File.Exists(_mockFs.Path.Combine(projectPath, "Assets") + ".meta"), Is.True);
        Assert.That(_mockFs.File.Exists(_mockFs.Path.Combine(projectPath, "Assets", "Scripts") + ".meta"), Is.True);
    }

    [Test]
    public async Task ScaffoldProject_CreatesProjectVersion()
    {
        string projectPath = await _service.ScaffoldProjectAsync("VersionTest", @"C:\output", "2023.2.0f1");

        string versionContent = _mockFs.File.ReadAllText(
            _mockFs.Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"));
        Assert.That(versionContent, Does.Contain("2023.2.0f1"));
    }

    [Test]
    public async Task ScaffoldProject_IsIdempotent()
    {
        string path1 = await _service.ScaffoldProjectAsync("Idem", @"C:\output");
        string path2 = await _service.ScaffoldProjectAsync("Idem", @"C:\output");
        Assert.That(path1, Is.EqualTo(path2));
    }

    [Test]
    public async Task ScaffoldProject_SanitizesName()
    {
        string path = await _service.ScaffoldProjectAsync("My Game!!! (alpha)", @"C:\output");
        Assert.That(_mockFs.Path.GetFileName(path), Does.Not.Contain(" "));
        Assert.That(_mockFs.Path.GetFileName(path), Does.Not.Contain("!"));
    }

    [Test]
    public async Task CreateFolder_CreatesWithMeta()
    {
        string projectPath = await _service.ScaffoldProjectAsync("FolderTest", @"C:\output");
        await _service.CreateFolderAsync(projectPath, "Assets/Custom");

        string folder = _mockFs.Path.Combine(projectPath, "Assets", "Custom");
        Assert.That(_mockFs.Directory.Exists(folder), Is.True);
        Assert.That(_mockFs.File.Exists(folder + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveScript_CreatesFileAndMeta()
    {
        // First scaffold so the project structure exists
        string proj = await _service.ScaffoldProjectAsync("ScriptTest", @"C:\output");
        await _service.SaveScriptAsync(proj, "Player.cs", "using UnityEngine;\npublic class Player : MonoBehaviour {}");

        string scriptPath = _mockFs.Path.Combine(proj, "Assets", "Scripts", "Player.cs");
        Assert.That(_mockFs.File.Exists(scriptPath), Is.True);
        Assert.That(_mockFs.File.Exists(scriptPath + ".meta"), Is.True);

        string meta = _mockFs.File.ReadAllText(scriptPath + ".meta");
        Assert.That(meta, Does.Contain("MonoImporter:"));
    }

    [Test]
    public async Task SaveTextAsset_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TextTest", @"C:\output");
        await _service.SaveTextAssetAsync(proj, "info.txt", "Hello World");

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Text", "info.txt");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);
        Assert.That(_mockFs.File.Exists(filePath + ".meta"), Is.True);
    }

    [Test]
    public async Task SaveTexture_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("TexTest", @"C:\output");
        string base64 = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _service.SaveTextureAsync(proj, "sprite.png", base64);

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Textures", "sprite.png");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);

        string meta = _mockFs.File.ReadAllText(filePath + ".meta");
        Assert.That(meta, Does.Contain("TextureImporter:"));
    }

    [Test]
    public async Task SaveAudio_CreatesFileAndMeta()
    {
        string proj = await _service.ScaffoldProjectAsync("AudioTest", @"C:\output");
        string base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xFB, 0x90 });
        await _service.SaveAudioAsync(proj, "sfx.mp3", base64);

        string filePath = _mockFs.Path.Combine(proj, "Assets", "Audio", "sfx.mp3");
        Assert.That(_mockFs.File.Exists(filePath), Is.True);

        string meta = _mockFs.File.ReadAllText(filePath + ".meta");
        Assert.That(meta, Does.Contain("AudioImporter:"));
    }

    [Test]
    public async Task GetProjectInfo_ReturnsCorrectJson()
    {
        string proj = await _service.ScaffoldProjectAsync("InfoTest", @"C:\output", "2022.3.0f1");
        string info = await _service.GetProjectInfoAsync(proj);

        Assert.That(info, Does.Contain("InfoTest"));
        Assert.That(info, Does.Contain("2022.3.0f1"));
        Assert.That(info, Does.Contain("\"hasAssets\":true"));
    }

    [Test]
    public async Task ValidateCSharp_ValidCode_ReturnsTrue()
    {
        string code = "using UnityEngine;\npublic class Player : MonoBehaviour {\n    void Start() { }\n}";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":true"));
    }

    [Test]
    public async Task ValidateCSharp_MissingBrace_ReturnsFalse()
    {
        string code = "using UnityEngine;\npublic class Player : MonoBehaviour {\n    void Start() { }";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":false"));
        Assert.That(result, Does.Contain("closing brace"));
    }

    [Test]
    public async Task ValidateCSharp_NoClassKeyword_ReturnsFalse()
    {
        string code = "using UnityEngine;\nvoid Main() { }";
        string result = await _service.ValidateCSharpAsync(code);
        Assert.That(result, Does.Contain("\"isValid\":false"));
    }

    [Test]
    public async Task AddPackages_CreatesManifest()
    {
        string proj = await _service.ScaffoldProjectAsync("PkgTest", @"C:\output");
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
        Assert.That(manifest, Does.Contain("3.0.6"));
    }

    [Test]
    public async Task AddPackages_MergesWithExisting()
    {
        string proj = await _service.ScaffoldProjectAsync("MergeTest", @"C:\output");
        await _service.AddPackagesAsync(proj, "{\"com.unity.urp\":\"14.0.0\"}");
        await _service.AddPackagesAsync(proj, "{\"com.unity.textmeshpro\":\"3.0.6\"}");

        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.urp"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
    }

    [Test]
    public async Task InstallPackagesAsync_AddsToManifest()
    {
        string proj = await _service.ScaffoldProjectAsync("InstallPkg", @"C:\output");
        string json = await _service.InstallPackagesAsync(proj, new List<string> { "com.unity.render-pipelines.universal", "com.unity.textmeshpro" });

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("installed"));
        string manifest = _mockFs.File.ReadAllText(_mockFs.Path.Combine(proj, "Packages", "manifest.json"));
        Assert.That(manifest, Does.Contain("com.unity.render-pipelines.universal"));
        Assert.That(manifest, Does.Contain("com.unity.textmeshpro"));
    }

    [Test]
    public async Task CreateDefaultSceneAsync_CreatesSceneAndPrefab()
    {
        string proj = await _service.ScaffoldProjectAsync("DefaultScene", @"C:\output");
        string json = await _service.CreateDefaultSceneAsync(proj, "MainScene");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("scene_path"));
        Assert.That(json, Does.Contain("prefab_path"));
        Assert.That(json, Does.Contain("Assets/Scenes/MainScene.unity"));
        Assert.That(json, Does.Contain("Assets/Prefabs/Ground.prefab"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainScene.unity");
        string prefabPath = _mockFs.Path.Combine(proj, "Assets", "Prefabs", "Ground.prefab");
        Assert.That(_mockFs.File.Exists(scenePath), Is.True);
        Assert.That(_mockFs.File.Exists(prefabPath), Is.True);
        Assert.That(_mockFs.File.Exists(scenePath + ".meta"), Is.True);
        Assert.That(_mockFs.File.Exists(prefabPath + ".meta"), Is.True);

        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("Main Camera"));
        Assert.That(sceneContent, Does.Contain("Directional Light"));
        Assert.That(sceneContent, Does.Contain("Ground"));
    }

    [Test]
    public async Task ValidateImportAsync_ReturnsStubJson()
    {
        string proj = await _service.ScaffoldProjectAsync("ValidateStub", @"C:\output");
        string json = await _service.ValidateImportAsync(proj);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("\"error_count\":0"));
        Assert.That(json, Does.Contain("\"warning_count\":0"));
    }

    // ---- UI authoring (Phase 1) ----

    [Test]
    public async Task CreateUiCanvasAsync_NewScene_CreatesFileWithCanvasAndEventSystem()
    {
        string proj = await _service.ScaffoldProjectAsync("UiCanvas", @"C:\output");
        string json = await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/MainMenu.unity");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("\"path\""));
        Assert.That(json, Does.Contain("MainMenu.unity"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainMenu.unity");
        Assert.That(_mockFs.File.Exists(scenePath), Is.True);
        Assert.That(_mockFs.File.Exists(scenePath + ".meta"), Is.True);

        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("Canvas"));
        Assert.That(sceneContent, Does.Contain("EventSystem"));
        Assert.That(sceneContent, Does.Contain("Main Camera"));
    }

    [Test]
    public async Task CreateUiLayoutAsync_AfterCanvas_AppendsHierarchyAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("UiLayout", @"C:\output");
        await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/MainMenu.unity");

        const string layoutJson = "{\"name\":\"MainMenu\",\"panels\":[{\"name\":\"RootPanel\",\"controls\":[{\"name\":\"StartButton\",\"type\":0,\"text\":\"Start Game\"}]}]}";
        string json = await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/MainMenu.unity", layoutJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("UI layout applied successfully"));

        string scenePath = _mockFs.Path.Combine(proj, "Assets", "Scenes", "MainMenu.unity");
        string sceneContent = _mockFs.File.ReadAllText(scenePath);
        Assert.That(sceneContent, Does.Contain("RootPanel"));
        Assert.That(sceneContent, Does.Contain("StartButton"));
    }

    [Test]
    public async Task CreateUiLayoutAsync_EmptyLayout_ReturnsSuccessWithWarning()
    {
        string proj = await _service.ScaffoldProjectAsync("UiLayoutEmpty", @"C:\output");
        await _service.CreateUiCanvasAsync(proj, "Assets/Scenes/Empty.unity");

        string json = await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/Empty.unity", "{\"name\":\"Empty\",\"panels\":[]}");

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("UiLayout.Empty"));
    }

    [Test]
    public void CreateUiLayoutAsync_TargetNotFound_Throws()
    {
        string proj = _mockFs.Path.GetFullPath(@"C:\proj");
        _mockFs.Directory.CreateDirectory(proj);
        _mockFs.Directory.CreateDirectory(_mockFs.Path.Combine(proj, "Assets"));
        _mockFs.Directory.CreateDirectory(_mockFs.Path.Combine(proj, "Assets", "Scenes"));

        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _service.CreateUiLayoutAsync(proj, "Assets/Scenes/NoSuch.unity", "{\"name\":\"X\",\"panels\":[]}"));
    }

    // ---- Navigation (Phase 2) ----

    [Test]
    public async Task ConfigureNavmeshAsync_ValidJson_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("NavMesh", @"C:\output");
        const string configJson = "{\"agentRadius\":0.5,\"agentHeight\":2,\"agentSlope\":45,\"agentClimb\":0.4,\"cellSize\":0.1,\"cellHeight\":0.2,\"manualVoxelSize\":false}";
        string json = await _service.ConfigureNavmeshAsync(proj, configJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        Assert.That(json, Does.Contain("NavMeshConfig"));

        string configPath = _mockFs.Path.Combine(proj, "Assets", "Settings", "NavMeshConfig.json");
        Assert.That(_mockFs.File.Exists(configPath), Is.True);
        Assert.That(_mockFs.File.Exists(configPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(configPath);
        Assert.That(content, Does.Contain("agentRadius"));
        Assert.That(content, Does.Contain("0.5"));
    }

    [Test]
    public async Task ConfigureNavmeshAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("NavMeshInvalid", @"C:\output");
        string json = await _service.ConfigureNavmeshAsync(proj, "{ invalid json }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("NavMeshConfig.InvalidJson"));
    }

    [Test]
    public async Task CreateWaypointGraphAsync_ValidGraph_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Waypoints", @"C:\output");
        const string graphJson = "{\"name\":\"PatrolRoute\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}},{\"id\":\"B\",\"position\":{\"x\":5,\"y\":0,\"z\":0}}],\"edges\":[{\"from\":\"A\",\"to\":\"B\",\"bidirectional\":true}]}";
        string json = await _service.CreateWaypointGraphAsync(proj, "Assets/Data/PatrolRoute.waypoints.json", graphJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        Assert.That(json, Does.Contain("PatrolRoute"));

        string graphPath = _mockFs.Path.Combine(proj, "Assets", "Data", "PatrolRoute.waypoints.json");
        Assert.That(_mockFs.File.Exists(graphPath), Is.True);
        Assert.That(_mockFs.File.Exists(graphPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(graphPath);
        Assert.That(content, Does.Contain("PatrolRoute"));
        Assert.That(content, Does.Contain("A"));
        Assert.That(content, Does.Contain("B"));
    }

    [Test]
    public async Task CreateWaypointGraphAsync_InvalidGraph_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("WaypointsInvalid", @"C:\output");
        const string graphJson = "{\"name\":\"X\",\"nodes\":[{\"id\":\"A\",\"position\":{\"x\":0,\"y\":0,\"z\":0}}],\"edges\":[{\"from\":\"A\",\"to\":\"C\",\"bidirectional\":false}]}";
        string json = await _service.CreateWaypointGraphAsync(proj, "Assets/Data/X.waypoints.json", graphJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("WaypointGraph.InvalidEdge"));
        Assert.That(json, Does.Contain("C"));
    }

    // ---- Input (Phase 2) ----

    [Test]
    public async Task CreateInputActionsAsync_ValidJson_WritesInputAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("InputActions", @"C:\output");
        const string actionsJson = "{\"name\":\"PlayerControls\",\"maps\":[{\"name\":\"Gameplay\",\"actions\":[{\"name\":\"Move\",\"type\":\"Value\",\"expectedControlType\":\"Vector2\"},{\"name\":\"Jump\",\"type\":\"Button\",\"expectedControlType\":\"Button\"}],\"bindings\":[{\"action\":\"Move\",\"path\":\"<Keyboard>/wasd\"},{\"action\":\"Jump\",\"path\":\"<Keyboard>/space\"}]}]}";
        string json = await _service.CreateInputActionsAsync(proj, "Assets/Input/PlayerControls.inputactions", actionsJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Input", "PlayerControls.inputactions");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("PlayerControls"));
        Assert.That(content, Does.Contain("Gameplay"));
        Assert.That(content, Does.Contain("Move"));
    }

    [Test]
    public async Task CreateInputActionsAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("InputInvalid", @"C:\output");
        string json = await _service.CreateInputActionsAsync(proj, "Assets/Input/Invalid.inputactions", "{ broken }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("InputActions.InvalidJson"));
    }

    // ---- Basic Animation (Phase 2) ----

    [Test]
    public async Task CreateBasicAnimatorAsync_ValidDefinition_WritesAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("Animator", @"C:\output");
        const string animatorJson = "{\"name\":\"CharacterAnimator\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":\"Assets/Animations/Idle.anim\"},{\"name\":\"Run\",\"clip\":\"Assets/Animations/Run.anim\"}],\"transitions\":[{\"from\":\"Idle\",\"to\":\"Run\",\"condition\":\"Speed > 0.1\"}]}";
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/Character.animator.json", animatorJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Animations", "Character.animator.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("CharacterAnimator"));
        Assert.That(content, Does.Contain("Idle"));
        Assert.That(content, Does.Contain("Run"));
    }

    [Test]
    public async Task CreateBasicAnimatorAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("AnimatorInvalid", @"C:\output");
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/X.animator.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("BasicAnimator.InvalidJson"));
    }

    [Test]
    public async Task CreateBasicAnimatorAsync_MissingClip_ReportsWarning()
    {
        string proj = await _service.ScaffoldProjectAsync("AnimatorMissingClip", @"C:\output");
        const string animatorJson = "{\"name\":\"X\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":\"Assets/Animations/NonExistent.anim\"}],\"transitions\":[]}";
        string json = await _service.CreateBasicAnimatorAsync(proj, "Assets/Animations/X.animator.json", animatorJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("warnings"));
        Assert.That(json, Does.Contain("BasicAnimator.MissingClip"));
        Assert.That(json, Does.Contain("NonExistent"));
    }

    // ---- Advanced Animation & Timelines (Phase 3) ----

    [Test]
    public async Task CreateAdvancedAnimatorAsync_ValidDefinition_WritesAsset()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimator", @"C:\output");
        const string advancedJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"Locomotion\",\"states\":[{\"name\":\"Locomotion\",\"clip\":\"Assets/Animations/Locomotion.anim\"}],\"subStateMachines\":[]}]}";
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/CharacterAdvanced.animator.json", advancedJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Animations", "CharacterAdvanced.animator.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("Base Layer"));
        Assert.That(content, Does.Contain("Locomotion"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimatorInvalid", @"C:\output");
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/Bad.animator.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("AdvancedAnimator.InvalidJson"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidStructure_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("AdvancedAnimatorBadStructure", @"C:\output");
        const string advancedJson = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"MissingState\",\"states\":[],\"subStateMachines\":[]}]}";
        string json = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/BadStructure.animator.json", advancedJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("AdvancedAnimator.InvalidLayer"));
        Assert.That(json, Does.Contain("MissingState"));
    }

    [Test]
    public async Task CreateTimelineAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Timeline", @"C:\output");
        const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/IntroPose.anim\",\"start\":0.0,\"duration\":2.0}]}]}";
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/IntroCutscene.timeline.json", timelineJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));
        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Timelines", "IntroCutscene.timeline.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("IntroCutscene"));
        Assert.That(content, Does.Contain("IntroPose"));
    }

    [Test]
    public async Task CreateTimelineAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("TimelineInvalid", @"C:\output");
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/Bad.timeline.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Timeline.InvalidJson"));
    }

    [Test]
    public async Task CreateTimelineAsync_MissingAssets_EmitsWarnings()
    {
        string proj = await _service.ScaffoldProjectAsync("TimelineMissingAssets", @"C:\output");
        const string timelineJson = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/Missing.anim\",\"start\":0.0,\"duration\":2.0}]},{\"type\":\"Audio\",\"binding\":\"MusicSource\",\"clips\":[{\"name\":\"IntroMusic\",\"audio\":\"Assets/Audio/Missing.ogg\",\"start\":0.0,\"duration\":30.0}]}]}";
        string json = await _service.CreateTimelineAsync(proj, "Assets/Timelines/IntroCutscene.timeline.json", timelineJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("warnings"));
        Assert.That(json, Does.Contain("Timeline.MissingClip"));
        Assert.That(json, Does.Contain("Timeline.MissingAudio"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsSetup", @"C:\output");
        const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0},{\"name\":\"Spine\",\"colliderType\":\"Box\",\"mass\":8.0}],\"joints\":[{\"name\":\"SpineJoint\",\"type\":\"Configurable\",\"bone\":\"Spine\",\"connectedBodyName\":\"Hips\"}]}";
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("path"));

        string assetPath = _mockFs.Path.Combine(proj, "Assets", "Physics", "HumanoidRagdoll.physics.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("HumanoidRagdoll"));
        Assert.That(content, Does.Contain("Hips"));
        Assert.That(content, Does.Contain("Spine"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsInvalidJson", @"C:\output");
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/Invalid.physics.json", "{ invalid }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("PhysicsSetup.InvalidJson"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidReferences_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("PhysicsInvalidRefs", @"C:\output");
        const string physicsJson = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0}],\"joints\":[{\"name\":\"BadJoint\",\"type\":\"Hinge\",\"bone\":\"Spine\",\"connectedBodyName\":\"Head\"}]}";
        string json = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/HumanoidRagdoll.physics.json", physicsJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("PhysicsSetup.InvalidReference"));
        Assert.That(json, Does.Contain("Spine"));
        Assert.That(json, Does.Contain("Head"));
    }

    // ---- VFX / particles (Phase 3) ----

    [Test]
    public async Task CreateVfxAssetAsync_ValidDefinition_WritesAssetAndReturnsSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Vfx", @"C:\output");
        const string vfxJson = "{\"name\":\"ExplosionSmall\",\"duration\":1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[{\"time\":0.0,\"count\":50}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/ExplosionSmall.vfx.json", vfxJson);

        Assert.That(json, Does.Contain("\"success\":true"));
        Assert.That(json, Does.Contain("ExplosionSmall"));

        string assetPath = _mockFs.Path.Combine(proj, "Assets", "VFX", "ExplosionSmall.vfx.json");
        Assert.That(_mockFs.File.Exists(assetPath), Is.True);
        Assert.That(_mockFs.File.Exists(assetPath + ".meta"), Is.True);
        string content = _mockFs.File.ReadAllText(assetPath);
        Assert.That(content, Does.Contain("ExplosionSmall"));
        Assert.That(content, Does.Contain("\"duration\": 1"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidJson_ReturnsValidationError()
    {
        string proj = await _service.ScaffoldProjectAsync("VfxInvalidJson", @"C:\output");
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/Bad.vfx.json", "{ invalid json }");

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Vfx.InvalidJson"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidParameters_ReturnsValidationErrors()
    {
        string proj = await _service.ScaffoldProjectAsync("VfxInvalidParams", @"C:\output");
        const string vfxJson = "{\"name\":\"ExplosionBad\",\"duration\":-1.0,\"looping\":false,\"startLifetime\":-0.5,\"startSpeed\":-5.0,\"startSize\":-1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":-1,\"bursts\":[{\"time\":-0.1,\"count\":-10}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string json = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/ExplosionBad.vfx.json", vfxJson);

        Assert.That(json, Does.Contain("\"success\":false"));
        Assert.That(json, Does.Contain("Vfx.InvalidParameters"));
        Assert.That(json, Does.Contain("validation failed"));
    }

    /// <summary>
    /// Verifies that all operations use MockFileSystem only: the path we "create" in the mock
    /// must not exist on the real filesystem. If this test fails, something is writing to physical disk.
    /// See Docs/validation-pipeline-and-tests.md §6 Test Isolation.
    /// </summary>
    [Test]
    public async Task AllOperations_UseMockFileSystem_NoPhysicalDiskWritten()
    {
        const string sentinelPath = "UnityMcp_TestIsolation_Sentinel_DoNotCreate";
        string outputRoot = _mockFs.Path.Combine(_mockFs.Path.GetTempPath(), sentinelPath);
        string proj = await _service.ScaffoldProjectAsync("Proj", outputRoot);
        await _service.CreateDefaultSceneAsync(proj, "MainScene");

        // All I/O went through _mockFs. The real filesystem must NOT contain this path.
        bool existsOnRealDisk = System.IO.Directory.Exists(proj);
        Assert.That(existsOnRealDisk, Is.False,
            "Test isolation broken: project path exists on real filesystem. Ensure FileUnityService is constructed with MockFileSystem only.");
    }
}

