using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityMcp.Core.Interfaces;
using UnityMcp.Core.Models;

namespace UnityMcp.Infrastructure.Transport;

/// <summary>
/// Standard Input/Output transport implementation for MCP.
/// </summary>
public class StdioMcpTransport : IMcpTransport
{
    private readonly ILogger<StdioMcpTransport> _logger;
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;

    public StdioMcpTransport(ILogger<StdioMcpTransport> logger)
    {
        _logger = logger;
        _inputStream = Console.OpenStandardInput();
        _outputStream = Console.OpenStandardOutput();
    }

    /// <inheritdoc />
    public async Task StartAsync(Func<McpRequest, Task<McpResponse>> requestHandler, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(_inputStream);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(line);
                if (request != null)
                {
                    var response = await requestHandler(request);
                    if (response != null) // Notifications don't have responses
                    {
                        await SendAsync(response, cancellationToken);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON-RPC message");
                // Send parse error response...
                await SendAsync(new McpResponse 
                { 
                    JsonRpc = McpConstants.JsonRpcVersion, 
                    Error = new McpError { Code = McpConstants.ErrorCodes.ParseError, Message = "Parse error" }, 
                    Id = null 
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request");
            }
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await _outputStream.FlushAsync(cancellationToken);
    }
}
