using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;
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
        var result = await UnityTools.CreateScene(_unityService, "Scenes/TestScene.unity");
        await _unityService.Received(1).CreateSceneAsync("Scenes/TestScene.unity", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/TestScene.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateScript
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateScript_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScript(_unityService, "Scripts/Player.cs", "Player");
        await _unityService.Received(1).CreateScriptAsync("Scripts/Player.cs", "Player", null, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    [Test]
    public async Task CreateScript_WithAIContent_PassesContentToService()
    {
        string aiCode = "using UnityEngine;\npublic class Player : MonoBehaviour { void Update() { } }";
        var result = await UnityTools.CreateScript(_unityService, "Scripts/Player.cs", "Player", aiCode);
        await _unityService.Received(1).CreateScriptAsync("Scripts/Player.cs", "Player", aiCode, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
    }

    // -----------------------------------------------------------------------
    // ListAssets
    // -----------------------------------------------------------------------

    [Test]
    public async Task ListAssets_ValidRequest_ReturnsAssetsList()
    {
        var expected = new List<string> { "Assets/Textures/Player.png", "Assets/Textures/Ground.png" };
        _unityService.ListAssetsAsync("Assets/Textures", "*.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<string>)expected));

        var result = await UnityTools.ListAssets(_unityService, "Assets/Textures", "*.png");
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task ListAssets_NoMatches_ReturnsEmptyList()
    {
        _unityService.ListAssetsAsync("Assets/Audio", "*.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Enumerable.Empty<string>()));
        var result = await UnityTools.ListAssets(_unityService, "Assets/Audio", "*.wav");
        Assert.That(result, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // BuildProject
    // -----------------------------------------------------------------------

    [Test]
    public async Task BuildProject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.BuildProject(_unityService, "Win64", "C:/Builds/Output");
        await _unityService.Received(1).BuildProjectAsync("Win64", "C:/Builds/Output", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Win64"));
    }

    // -----------------------------------------------------------------------
    // CreateAsset
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateAsset_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateAsset(_unityService, "Materials/Test.mat", "content");
        await _unityService.Received(1).CreateAssetAsync("Materials/Test.mat", "content", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Test.mat"));
    }

    [Test]
    public async Task CreateAsset_EmptyContent_DefaultsToEmptyString()
    {
        var result = await UnityTools.CreateAsset(_unityService, "Materials/Empty.mat");
        await _unityService.Received(1).CreateAssetAsync("Materials/Empty.mat", "", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Empty.mat"));
    }

    // -----------------------------------------------------------------------
    // CreateGameObject (legacy)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateGameObject_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateGameObject(_unityService, "Scenes/Main.unity", "Cube");
        await _unityService.Received(1).CreateGameObjectAsync("Scenes/Main.unity", "Cube", Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Cube"));
    }

    // -----------------------------------------------------------------------
    // CreateDetailedScene (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateDetailedScene_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """[{"name":"Camera","tag":"MainCamera","position":{"x":0,"y":1,"z":-10},"components":[{"type":"Camera"}]}]""";
        var result = await UnityTools.CreateDetailedScene(_unityService, "Assets/Scenes/Level1.unity", json);
        await _unityService.Received(1).CreateDetailedSceneAsync("Assets/Scenes/Level1.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Level1.unity"));
    }

    // -----------------------------------------------------------------------
    // AddGameObject (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task AddGameObject_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Sphere","position":{"x":3,"y":0,"z":0},"components":[{"type":"MeshFilter","mesh":"Sphere"},{"type":"MeshRenderer"}]}""";
        var result = await UnityTools.AddGameObject(_unityService, "Scenes/Main.unity", json);
        await _unityService.Received(1).AddGameObjectToSceneAsync("Scenes/Main.unity", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/Main.unity"));
    }

    // -----------------------------------------------------------------------
    // CreateMaterial (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreateMaterial_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"RedMetal","color":{"r":1,"g":0,"b":0,"a":1},"metallic":0.8,"smoothness":0.6}""";
        var result = await UnityTools.CreateMaterial(_unityService, "Assets/Materials/RedMetal.mat", json);
        await _unityService.Received(1).CreateMaterialAsync("Assets/Materials/RedMetal.mat", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("RedMetal.mat"));
    }

    // -----------------------------------------------------------------------
    // CreatePrefab (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task CreatePrefab_ValidJson_CallsServiceAndReturnsMessage()
    {
        string json = """{"name":"Enemy","components":[{"type":"MeshFilter","mesh":"Capsule"},{"type":"Rigidbody","mass":2}]}""";
        var result = await UnityTools.CreatePrefab(_unityService, "Assets/Prefabs/Enemy.prefab", json);
        await _unityService.Received(1).CreatePrefabAsync("Assets/Prefabs/Enemy.prefab", json, Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Enemy.prefab"));
    }

    // -----------------------------------------------------------------------
    // ReadAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task ReadAsset_ReturnsFileContent()
    {
        _unityService.ReadAssetAsync("Assets/Scripts/Player.cs", Arg.Any<CancellationToken>())
            .Returns("using UnityEngine; class Player {}");

        var result = await UnityTools.ReadAsset(_unityService, "Assets/Scripts/Player.cs");
        Assert.That(result, Does.Contain("using UnityEngine"));
    }

    // -----------------------------------------------------------------------
    // DeleteAsset (NEW)
    // -----------------------------------------------------------------------

    [Test]
    public async Task DeleteAsset_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.DeleteAsset(_unityService, "Assets/Old/Legacy.cs");
        await _unityService.Received(1).DeleteAssetAsync("Assets/Old/Legacy.cs", Arg.Any<CancellationToken>());
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
