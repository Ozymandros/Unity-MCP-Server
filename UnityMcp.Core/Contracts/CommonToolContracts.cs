using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Standard result envelope for Unity MCP tools that need structured status, data, warnings,
/// errors, and remediation guidance.
/// </summary>
/// <typeparam name="TData">The optional data payload type returned by the tool.</typeparam>
public sealed class ToolResultEnvelope<TData>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("data")]
    public TData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<UnityMcpError> Errors { get; init; } = Array.Empty<UnityMcpError>();

    [JsonPropertyName("warnings")]
    public IReadOnlyList<UnityMcpError> Warnings { get; init; } = Array.Empty<UnityMcpError>();

    [JsonPropertyName("suggestedRemediation")]
    public string? SuggestedRemediation { get; init; }
}

/// <summary>
/// Describes the current server runtime and feature modes for agent capability planning.
/// </summary>
public sealed class UnityServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Unity MCP Server";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "unknown";

    [JsonPropertyName("transport")]
    public string Transport { get; init; } = "stdio";

    [JsonPropertyName("backend")]
    public string Backend { get; init; } = "file";

    [JsonPropertyName("editorAvailable")]
    public bool EditorAvailable { get; init; }

    [JsonPropertyName("editorPath")]
    public string? EditorPath { get; init; }

    [JsonPropertyName("liveBridgeConnected")]
    public bool LiveBridgeConnected { get; init; }
}

/// <summary>
/// Describes a feature family exposed by the server and whether it is native, file-only,
/// compatibility-only, or unavailable.
/// </summary>
public sealed class UnityCapability
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Capability manifest returned to agents for runtime planning.
/// </summary>
public sealed class UnityCapabilityManifest
{
    [JsonPropertyName("server")]
    public UnityServerInfo Server { get; init; } = new();

    [JsonPropertyName("capabilities")]
    public IReadOnlyList<UnityCapability> Capabilities { get; init; } = Array.Empty<UnityCapability>();

    [JsonPropertyName("notes")]
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}
