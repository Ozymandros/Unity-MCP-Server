using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class CreateAssetHandlerTests
{
    private IUnityService _unityService;
    private CreateAssetHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new CreateAssetHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_CallsCreateAssetAsync()
    {
        // Arrange
        var parameters = new CreateAssetParameters
        {
            Path = "Assets/Data/Config.json",
            Content = "{ \"key\": \"value\" }"
        };

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        await _unityService.Received(1).CreateAssetAsync(
            Arg.Is<string>(x => x == "Assets/Data/Config.json"),
            Arg.Is<string>(x => x == "{ \"key\": \"value\" }"),
            Arg.Any<CancellationToken>()
        );
        Assert.That(result, Does.Contain("Asset created at Assets/Data/Config.json"));
    }
}
