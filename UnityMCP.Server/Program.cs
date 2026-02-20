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
        // ELIMINA qualsevol Console.WriteLine d'aquí!

        var host = Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime(options => options.SuppressStatusMessages = true)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders(); // Netegem logs automàtics
                logging.AddConsole(options =>
                {
                    // Forcem que tot el "soroll" de log vagi per la via d'error (Stderr)
                    // perquè no interfereixi amb el JSON de l'MCP
                    options.LogToStandardErrorThreshold = LogLevel.Trace;
                });
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IUnityService, FileUnityService>();
                services.AddSingleton<IMcpTransport, StdioMcpTransport>(); // Protocol oficial

                // Handlers necessaris
                services.AddSingleton<IMcpHandler, InitializeHandler>();
                services.AddSingleton<IMcpHandler, ListToolsHandler>();
                services.AddSingleton<IMcpHandler, CreateSceneHandler>();
                services.AddSingleton<IMcpHandler, CreateScriptHandler>();
                services.AddSingleton<IMcpHandler, ListAssetsHandler>();
                services.AddSingleton<IMcpHandler, BuildProjectHandler>();
                services.AddSingleton<IMcpHandler, CreateGameObjectHandler>();
                services.AddSingleton<IMcpHandler, CreateAssetHandler>();
                services.AddSingleton<McpRouter>();
                // Register CallToolHandler with router injection
                services.AddSingleton<IMcpHandler>(sp => new CallToolHandler(sp.GetRequiredService<McpRouter>()));
                services.AddHostedService<McpHostedService>(); // Motor del servidor
            })
            .Build();

        try
        {
            // Arrenca el servidor en silenci total
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // Els errors van a Stderr per seguretat
            await Console.Error.WriteLineAsync($"FATAL: {ex.Message}");
        }
    }
}
