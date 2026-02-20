using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.IO.Abstractions;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    // Route all log output to stderr so it doesn't interfere with the stdio MCP protocol
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IUnityService, FileUnityService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(UnityMcp.Application.Tools.UnityTools).Assembly);

await builder.Build().RunAsync();
