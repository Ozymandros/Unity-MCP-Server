using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// High-level categories for Unity MCP operation errors.
/// Used to distinguish validation issues from IO, contract, and internal failures.
/// </summary>
public enum UnityMcpErrorCategory
{
    Validation,
    Io,
    Contract,
    ExternalTool,
    Internal,
}

/// <summary>
/// Standardized error payload for Unity MCP operations.
/// This is intended to be additive and backwards compatible with existing JSON contracts.
/// </summary>
public sealed class UnityMcpError
{
    public UnityMcpErrorCategory Category { get; init; }

    /// <summary>
    /// Stable, machine-readable code (e.g. "InstallPackages.Failure").
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description suitable for logs and UI.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional opaque details object for additional context (file paths, stack traces, etc.).
    /// Not guaranteed to be stable across versions.
    /// </summary>
    public object? Details { get; init; }
}

/// <summary>
/// Result contract for package installation tools.
/// Serialized JSON fields: success, installed, message, errors.
/// </summary>
public sealed class InstallPackagesResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("installed")]
    public IReadOnlyList<string> Installed { get; init; } = Array.Empty<string>();

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<UnityMcpError> Errors { get; init; } = Array.Empty<UnityMcpError>();
}

/// <summary>
/// Result contract for default scene creation tools.
/// Serialized JSON fields: success, scene_path, prefab_path, message, errors.
/// </summary>
public sealed class DefaultSceneResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("scene_path")]
    public string? ScenePath { get; init; }

    [JsonPropertyName("prefab_path")]
    public string? PrefabPath { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<UnityMcpError> Errors { get; init; } = Array.Empty<UnityMcpError>();
}

/// <summary>
/// Result contract for URP configuration tools.
/// Serialized JSON fields: success, message, errors.
/// </summary>
public sealed class UrpConfigurationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<UnityMcpError> Errors { get; init; } = Array.Empty<UnityMcpError>();
}

/// <summary>
/// Result contract for import / compilation validation.
/// Serialized JSON fields: success, error_count, warning_count, errors, warnings, message.
/// </summary>
public sealed class ImportValidationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error_count")]
    public int ErrorCount { get; init; }

    [JsonPropertyName("warning_count")]
    public int WarningCount { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<UnityMcpError> Errors { get; init; } = Array.Empty<UnityMcpError>();

    [JsonPropertyName("warnings")]
    public IReadOnlyList<UnityMcpError> Warnings { get; init; } = Array.Empty<UnityMcpError>();

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

