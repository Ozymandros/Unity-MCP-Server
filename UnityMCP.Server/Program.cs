using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityMcp.Application.Handlers.Core;
using UnityMcp.Application.Handlers.Tools;
using UnityMcp.Application.Routing;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;
using UnityMcp.Infrastructure.Transport;

namespace UnityMcp.Server;

/// <summary>
/// Main entry point for the application.
/// </summary>
class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                // Important: MCP uses Stdio, so we must NOT log to Console.Out
                // We log to Stderr or a file.
                logging.ClearProviders();
                logging.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });
                logging.AddDebug();
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Infrastructure
                services.AddSingleton<IUnityService, FileUnityService>();
                services.AddSingleton<IMcpTransport, StdioMcpTransport>();
                services.AddSingleton<IProcessRunner, ProcessRunner>();

                // Routable Handlers
                services.AddSingleton<IMcpHandler, InitializeHandler>();
                services.AddSingleton<IMcpHandler, ListToolsHandler>();
                services.AddSingleton<IMcpHandler, PingHandler>();
                services.AddSingleton<IMcpHandler, CreateSceneHandler>();
                services.AddSingleton<IMcpHandler, CreateScriptHandler>();
                services.AddSingleton<IMcpHandler, ListAssetsHandler>();
                services.AddSingleton<IMcpHandler, CreateGameObjectHandler>();
                services.AddSingleton<IMcpHandler, CreateAssetHandler>();
                services.AddSingleton<IMcpHandler, BuildProjectHandler>();

                // Router
                services.AddSingleton<McpRouter>();

                // Hosted Service
                services.AddHostedService<McpHostedService>();
            })
            .Build();

        // Send MCP handshake immediately for VS Code detection
        Console.WriteLine("{\"mcpVersion\":\"1.0\",\"capabilities\":{}}");
        Console.Out.Flush();

        // Optional: Parse args to set project path in IUnityService
        var unityService = host.Services.GetRequiredService<IUnityService>();
        // Default to current directory if not specified
        string projectPath = Directory.GetCurrentDirectory(); 
        // Only strictly creating the UnityService, not initiating validity check until invoked
        await unityService.IsValidProjectAsync(projectPath);

        await host.RunAsync();
    }
}
