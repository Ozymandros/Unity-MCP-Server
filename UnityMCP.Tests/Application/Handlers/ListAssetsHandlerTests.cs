using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class ListAssetsHandlerTests
{
    private IUnityService _unityService;
    private ListAssetsHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new ListAssetsHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_ReturnsAssetsList()
    {
        // Arrange
        var parameters = new ListAssetsParameters
        {
            Path = "Assets/Textures",
            Pattern = "*.png"
        };
        var expectedAssets = new List<string> { "Assets/Textures/Player.png", "Assets/Textures/Ground.png" };
        
        _unityService.ListAssetsAsync("Assets/Textures", "*.png", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<string>)expectedAssets));

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        await _unityService.Received(1).ListAssetsAsync(
            "Assets/Textures",
            "*.png",
            Arg.Any<CancellationToken>()
        );
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result, Is.EquivalentTo(expectedAssets));
    }

    [Test]
    public async Task HandleAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var parameters = new ListAssetsParameters
        {
            Path = "Assets/Audio",
            Pattern = "*.wav"
        };
        
        _unityService.ListAssetsAsync("Assets/Audio", "*.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Enumerable.Empty<string>()));

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        Assert.That(result, Is.Empty);
    }
}
