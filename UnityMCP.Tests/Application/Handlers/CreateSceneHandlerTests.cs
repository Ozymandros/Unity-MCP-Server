using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class CreateSceneHandlerTests
{
    private IUnityService _unityService;
    private CreateSceneHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new CreateSceneHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_CallsCreateSceneAsync()
    {
        // Arrange
        var parameters = new CreateSceneParameters
        {
            Path = "Scenes/TestScene.unity"
        };

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        await _unityService.Received(1).CreateSceneAsync(
            Arg.Is<string>(x => x == "Scenes/TestScene.unity"),
            Arg.Any<CancellationToken>()
        );
        Assert.That(result, Does.Contain("Scenes/TestScene.unity"));
    }
}
