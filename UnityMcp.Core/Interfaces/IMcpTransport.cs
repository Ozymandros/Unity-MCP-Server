using System;
using System.Threading;
using System.Threading.Tasks;
using UnityMcp.Core.Models;

namespace UnityMcp.Core.Interfaces;

/// <summary>
/// Abstraction for the transport layer of the MCP protocol (Stdio, WebSocket, etc.).
/// </summary>
public interface IMcpTransport
{
    /// <summary>
    /// Starts listening for incoming messages.
    /// </summary>
    /// <param name="requestHandler">The function to handle incoming requests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(Func<McpRequest, Task<McpResponse>> requestHandler, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a notification or response to the client.
    /// </summary>
    /// <param name="message">The message object to serialize and send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(object message, CancellationToken cancellationToken);
}
