using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Tools;
using UnityMcp.Core.Interfaces;

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
