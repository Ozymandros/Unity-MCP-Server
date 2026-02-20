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

    #region Ping

    [Test]
    public void Ping_ReturnsPong()
    {
        var result = UnityTools.Ping();
        Assert.That(result, Is.EqualTo("pong"));
    }

    #endregion

    #region CreateScene

    [Test]
    public async Task CreateScene_ValidPath_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScene(_unityService, "Scenes/TestScene.unity");

        await _unityService.Received(1).CreateSceneAsync(
            "Scenes/TestScene.unity",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Scenes/TestScene.unity"));
    }

    #endregion

    #region CreateScript

    [Test]
    public async Task CreateScript_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateScript(
            _unityService, "Scripts/Player.cs", "Player");

        await _unityService.Received(1).CreateScriptAsync(
            "Scripts/Player.cs",
            "Player",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Player"));
        Assert.That(result, Does.Contain("Scripts/Player.cs"));
    }

    #endregion

    #region ListAssets

    [Test]
    public async Task ListAssets_ValidRequest_ReturnsAssetsList()
    {
        var expectedAssets = new List<string>
        {
            "Assets/Textures/Player.png",
            "Assets/Textures/Ground.png"
        };

        _unityService.ListAssetsAsync("Assets/Textures", "*.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<string>)expectedAssets));

        var result = await UnityTools.ListAssets(
            _unityService, "Assets/Textures", "*.png");

        await _unityService.Received(1).ListAssetsAsync(
            "Assets/Textures",
            "*.png",
            Arg.Any<CancellationToken>());
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result, Is.EquivalentTo(expectedAssets));
    }

    [Test]
    public async Task ListAssets_NoMatches_ReturnsEmptyList()
    {
        _unityService.ListAssetsAsync("Assets/Audio", "*.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Enumerable.Empty<string>()));

        var result = await UnityTools.ListAssets(
            _unityService, "Assets/Audio", "*.wav");

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region BuildProject

    [Test]
    public async Task BuildProject_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.BuildProject(
            _unityService, "Win64", "C:/Builds/Output");

        await _unityService.Received(1).BuildProjectAsync(
            "Win64",
            "C:/Builds/Output",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Win64"));
        Assert.That(result, Does.Contain("C:/Builds/Output"));
    }

    #endregion

    #region CreateGameObject

    [Test]
    public async Task CreateGameObject_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateGameObject(
            _unityService, "Scenes/Main.unity", "NewObject");

        await _unityService.Received(1).CreateGameObjectAsync(
            "Scenes/Main.unity",
            "NewObject",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("GameObject NewObject created in Scenes/Main.unity"));
    }

    [Test]
    public async Task CreateGameObject_NotSupported_ReturnsFriendlyError()
    {
        _unityService.CreateGameObjectAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotSupportedException("Unity CLI not found"));

        var result = await UnityTools.CreateGameObject(
            _unityService, "Scenes/Main.unity", "NewObject");

        Assert.That(result, Does.Contain("Feature not available in standalone mode"));
        Assert.That(result, Does.Contain("Unity CLI not found"));
    }

    #endregion

    #region CreateAsset

    [Test]
    public async Task CreateAsset_ValidParams_CallsServiceAndReturnsMessage()
    {
        var result = await UnityTools.CreateAsset(
            _unityService, "Materials/Test.mat", "content data");

        await _unityService.Received(1).CreateAssetAsync(
            "Materials/Test.mat",
            "content data",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Test.mat"));
    }

    [Test]
    public async Task CreateAsset_EmptyContent_DefaultsToEmptyString()
    {
        var result = await UnityTools.CreateAsset(
            _unityService, "Materials/Empty.mat");

        await _unityService.Received(1).CreateAssetAsync(
            "Materials/Empty.mat",
            "",
            Arg.Any<CancellationToken>());
        Assert.That(result, Does.Contain("Materials/Empty.mat"));
    }

    #endregion
}
