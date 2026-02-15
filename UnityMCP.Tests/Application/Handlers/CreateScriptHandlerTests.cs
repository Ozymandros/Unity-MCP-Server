using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests.Application.Handlers;

[TestFixture]
public class CreateScriptHandlerTests
{
    private IUnityService _unityService;
    private CreateScriptHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _unityService = Substitute.For<IUnityService>();
        _handler = new CreateScriptHandler(_unityService);
    }

    [Test]
    public async Task HandleAsync_ValidRequest_CallsCreateScriptAsync()
    {
        // Arrange
        var parameters = new CreateScriptParameters
        {
            Path = "Scripts/Player.cs",
            ScriptName = "Player"
        };

        // Act
        var result = await _handler.HandleAsync(parameters);

        // Assert
        // Strict verification of arguments
        await _unityService.Received(1).CreateScriptAsync(
            "Scripts/Player.cs",
            "Player",
            Arg.Any<CancellationToken>()
        );
        
        Assert.That(result, Does.Contain("Script Player created at Scripts/Player.cs"));
        Assert.That(result, Does.Contain("Scripts/Player.cs"));
    }
}
