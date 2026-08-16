using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Server metadata and capability discovery tools.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_get_server_info"), Description("Return Unity MCP server version, transport, backend mode, and Unity Editor availability as JSON.")]
    public static Task<string> GetServerInfo(
        IUnityService unityService,
        CancellationToken cancellationToken = default)
        => unityService.GetServerInfoAsync(cancellationToken);

    [McpServerTool(Name = "unity_get_capabilities"), Description("Return a structured capability manifest that distinguishes native, file-only, Editor-backed, and compatibility-surrogate features.")]
    public static Task<string> GetCapabilities(
        IUnityService unityService,
        CancellationToken cancellationToken = default)
        => unityService.GetCapabilitiesAsync(cancellationToken);
}
