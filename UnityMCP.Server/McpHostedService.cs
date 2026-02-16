using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Server;

/// <summary>
/// Hosted service that manages the lifecycle of the MCP server protocol loop.
/// </summary>
public class McpHostedService : BackgroundService
{
    private readonly IMcpTransport _transport;
    private readonly McpRouter _router;
    private readonly ILogger<McpHostedService> _logger;
    private readonly IUnityService _unityService;
    private readonly IHostApplicationLifetime _lifetime;

    public McpHostedService(
        IMcpTransport transport,
        McpRouter router,
        IUnityService unityService,
        ILogger<McpHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _transport = transport;
        _router = router;
        _logger = logger;
        _unityService = unityService;
        _lifetime = lifetime;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MCP handshake per VS Code MCP
        Console.WriteLine("{\"mcpVersion\":\"1.0\",\"capabilities\":{}}");

        // Redirigeix logs a STDERR per no trencar el protocol MCP
        Console.Error.WriteLine("Starting Unity MCP Server...");

        // Validate project path from args or env if needed, 
        // but we'll likely pass it in Initialize or just assume CWD for now.
        // For this implementation, we assume the CWD is the project root or passed via args.
        
        // In a real scenario, we might parse command line args here or in Program.cs
        // to set the UnityService project path.
        
        await _transport.StartAsync(async (request) =>
        {
            return await _router.RouteRequestAsync(request);
        }, stoppingToken);

        Console.Error.WriteLine("Unity MCP Server stopped.");
    }
}
