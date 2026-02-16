using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Core;

/// <summary>
/// Parameters for initialization.
/// </summary>
public class InitializeParameters
{
    [Required]
    public string ClientName { get; set; } = string.Empty;

    [Required]
    public string ClientVersion { get; set; } = string.Empty;
}

/// <summary>
/// Handles the 'initialize' handshake.
/// </summary>
public class InitializeHandler : McpHandlerBase<InitializeParameters, InitializeResult>
{
    /// <inheritdoc />
    public override string Method => McpMethods.Initialize;

    /// <inheritdoc />
    public override Task<InitializeResult> HandleAsync(InitializeParameters parameters)
    {
        return Task.FromResult(new InitializeResult
        {
            ProtocolVersion = McpConstants.ProtocolVersion,
            Server = new McpServerInfo
            {
                Name = "Unity MCP Server",
                Version = "1.0.0"
            },
            Capabilities = new McpCapabilities
            {
                Tools = new { }
            }
        });
    }
}
