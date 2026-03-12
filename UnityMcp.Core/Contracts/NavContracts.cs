using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// High-level NavMesh bake settings. Maps to project-wide or ScriptableObject-backed config.
/// </summary>
public sealed class NavMeshConfig
{
    [JsonPropertyName("agentRadius")]
    public float AgentRadius { get; init; } = 0.5f;

    [JsonPropertyName("agentHeight")]
    public float AgentHeight { get; init; } = 2f;

    [JsonPropertyName("agentSlope")]
    public float AgentSlope { get; init; } = 45f;

    [JsonPropertyName("agentClimb")]
    public float AgentClimb { get; init; } = 0.4f;

    [JsonPropertyName("cellSize")]
    public float CellSize { get; init; } = 0.1f;

    [JsonPropertyName("cellHeight")]
    public float CellHeight { get; init; } = 0.2f;

    [JsonPropertyName("manualVoxelSize")]
    public bool ManualVoxelSize { get; init; }
}

/// <summary>
/// 3D position for waypoint nodes.
/// </summary>
public sealed class NavVector3
{
    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("z")]
    public float Z { get; init; }
}

/// <summary>
/// A single node in a waypoint graph.
/// </summary>
public sealed class WaypointNode
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("position")]
    public NavVector3 Position { get; init; } = new();
}

/// <summary>
/// Directed or bidirectional edge between two waypoint nodes.
/// </summary>
public sealed class WaypointEdge
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; init; } = string.Empty;

    [JsonPropertyName("bidirectional")]
    public bool Bidirectional { get; init; }
}

/// <summary>
/// Root waypoint graph for simple AI pathing (patrol routes, etc.).
/// </summary>
public sealed class WaypointGraph
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "WaypointGraph";

    [JsonPropertyName("nodes")]
    public IReadOnlyList<WaypointNode> Nodes { get; init; } = new List<WaypointNode>();

    [JsonPropertyName("edges")]
    public IReadOnlyList<WaypointEdge> Edges { get; init; } = new List<WaypointEdge>();
}
