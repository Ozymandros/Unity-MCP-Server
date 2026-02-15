using System.Threading.Tasks;
using UnityMcp.Application.Models;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Tools;

/// <summary>
/// Handles the 'ping' request to check server connectivity.
/// </summary>
/// <summary>
/// Handles the 'ping' request to check server connectivity.
/// </summary>
public class PingHandler : McpHandlerBase<EmptyParameters, string>
{
    /// <inheritdoc />
    public override string Method => McpMethods.Ping;

    /// <inheritdoc />
    public override Task<string> HandleAsync(EmptyParameters parameters)
    {
        return Task.FromResult("pong");
    }
}
