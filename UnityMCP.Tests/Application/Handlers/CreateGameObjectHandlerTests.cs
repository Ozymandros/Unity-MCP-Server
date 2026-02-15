using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class CreateGameObjectHandlerTests
{
    private IUnityService _unityService;
    private CreateGameObjectHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new CreateGameObjectHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_CallsCreateGameObjectAsync()
    {
        // Arrange
        var parameters = new CreateGameObjectParameters
        {
            ScenePath = "Scenes/Main.unity",
            GameObjectName = "NewObject"
        };

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        // Strict verification of arguments
        await _unityService.Received(1).CreateGameObjectAsync(
            "Scenes/Main.unity",
            "NewObject",
            Arg.Any<CancellationToken>()
        );

        Assert.That(result, Does.Contain("GameObject NewObject created in Scenes/Main.unity"));
        Assert.That(result, Does.Contain("Scenes/Main.unity"));
    }

    [Test]
    public async Task HandleAsync_UnityServiceThrowsNotSupported_ReturnsFriendlyError()
    {
        // Arrange
        var parameters = new CreateGameObjectParameters
        {
            ScenePath = "Scenes/Main.unity",
            GameObjectName = "NewObject"
        };

        _unityService.When(x => x.CreateGameObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(x => { throw new System.NotSupportedException("Unity CLI not found"); });

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        Assert.That(result, Does.Contain("Feature not available in standalone mode"));
        Assert.That(result, Does.Contain("Unity CLI not found"));
    }
}
