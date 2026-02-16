using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Tests.Application;

[TestFixture]
public class McpRouterTests
{
    private IMcpHandler _mockHandler;
    private ILogger<McpRouter> _mockLogger;
    private McpRouter _router;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = Substitute.For<IMcpHandler>();
        _mockHandler.Method.Returns("test/method");
        
        _mockLogger = Substitute.For<ILogger<McpRouter>>();
        
        _router = new McpRouter(new[] { _mockHandler }, _mockLogger);
    }

    [Test]
    public async Task RouteRequestAsync_ValidMethod_CallsHandler()
    {
        // Arrange
        var parameters = new { foo = "bar" };
        var request = new McpRequest
        {
            Method = "test/method",
            Id = JsonSerializer.SerializeToElement(1),
            Params = JsonSerializer.SerializeToElement(parameters)
        };
        var expectedResult = JsonSerializer.SerializeToElement("success");
        
        _mockHandler.HandleAsync(Arg.Any<JsonElement?>()).Returns(Task.FromResult<JsonElement?>(expectedResult));

        // Act
        var response = await _router.RouteRequestAsync(request);

        // Assert
        Assert.That(response.Result.ToString(), Is.EqualTo(expectedResult.ToString()));
        Assert.That(response.Id.ToString(), Is.EqualTo(request.Id.ToString()));
        Assert.That(response.JsonRpc, Is.EqualTo(McpConstants.JsonRpcVersion));
    }

    [Test]
    public async Task RouteRequestAsync_UnknownMethod_ReturnsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "unknown/method",
            Id = JsonSerializer.SerializeToElement(1)
        };

        // Act
        var response = await _router.RouteRequestAsync(request);

        // Assert
        Assert.That(response.Error, Is.Not.Null);
        Assert.That(response.Error.Code, Is.EqualTo(McpConstants.ErrorCodes.MethodNotFound));
    }

    [Test]
    public async Task RouteRequestAsync_HandlerThrows_ReturnsInternalError()
    {
        // Arrange
        var request = new McpRequest
        {
            Method = "test/method",
            Id = JsonSerializer.SerializeToElement(1)
        };
        _mockHandler.HandleAsync(Arg.Any<JsonElement?>()).Returns(Task.FromException<JsonElement?>(new Exception("Fail")));

        // Act
        var response = await _router.RouteRequestAsync(request);

        // Assert
        Assert.That(response.Error, Is.Not.Null);
        Assert.That(response.Error.Code, Is.EqualTo(McpConstants.ErrorCodes.ServerErrorExecution));
        Assert.That(response.Error.Message, Is.EqualTo("Fail"));
    }
}
