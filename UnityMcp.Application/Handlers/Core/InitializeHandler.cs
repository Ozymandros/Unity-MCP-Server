using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Models;

namespace UnityMcp.Application.Handlers.Core;

using System.Text.Json.Serialization;

/// <summary>
/// Parameters for initialization.
/// </summary>
public class InitializeParameters
{
    [JsonPropertyName("protocolVersion")]
    [Required]
    public string ProtocolVersion { get; set; } = string.Empty;

    [JsonPropertyName("clientInfo")]
    [Required]
    public McpClientInfo ClientInfo { get; set; } = new();

    [JsonPropertyName("capabilities")]
    public object? Capabilities { get; set; }
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
