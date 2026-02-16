using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class BuildProjectHandlerTests
{
    private IUnityService _unityService;
    private BuildProjectHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new BuildProjectHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_CallsBuildProjectAsync()
    {
        // Arrange
        var parameters = new BuildProjectParameters
        {
            Target = "Win64",
            OutputPath = "Builds/Game.exe"
        };

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        await _unityService.Received(1).BuildProjectAsync(
            Arg.Is<string>(x => x == "Win64"),
            Arg.Is<string>(x => x == "Builds/Game.exe"),
            Arg.Any<CancellationToken>()
        );
        Assert.That(result, Does.Contain("Build initiated"));
    }
}
