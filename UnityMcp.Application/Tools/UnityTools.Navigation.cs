using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Navigation (NavMesh and waypoint graphs).</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_configure_navmesh"), Description(
        "Writes or updates a NavMesh configuration asset from JSON (agentRadius, agentHeight, agentSlope, etc.). " +
        "See NavContracts.NavMeshConfig. Returns JSON: success, path, message, errors.")]
    public static async Task<string> ConfigureNavmesh(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("NavMesh config JSON (NavMeshConfig schema)")]
        string configJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.ConfigureNavmeshAsync(projectPath, configJson, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_waypoint_graph"), Description(
        "Creates a waypoint graph asset for AI pathing (nodes and edges). " +
        "fileName can be path (e.g. Assets/Data/PatrolRoute.waypoints.json) or name. Returns JSON: success, path, message, errors.")]
    public static async Task<string> CreateWaypointGraph(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Asset file name or path under project for the waypoint graph")]
        string fileName,
        [Description("Waypoint graph JSON (name, nodes with id/position, edges with from/to/bidirectional)")]
        string graphJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateWaypointGraphAsync(projectPath, fileName, graphJson, cancellationToken);
    }
}
