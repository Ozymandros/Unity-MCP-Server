using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Lightweight GameObject entry returned by scene and prefab graph tools.
/// </summary>
public sealed class UnitySceneObject
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("fileId")]
    public string FileId { get; init; } = string.Empty;

    [JsonPropertyName("parentFileId")]
    public string? ParentFileId { get; init; }

    [JsonPropertyName("components")]
    public IReadOnlyList<string> Components { get; init; } = Array.Empty<string>();

    [JsonPropertyName("properties")]
    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
}

/// <summary>
/// Scene graph payload containing every discovered GameObject.
/// </summary>
public sealed class UnitySceneGraph
{
    [JsonPropertyName("scenePath")]
    public string ScenePath { get; init; } = string.Empty;

    [JsonPropertyName("objects")]
    public IReadOnlyList<UnitySceneObject> Objects { get; init; } = Array.Empty<UnitySceneObject>();
}

/// <summary>
/// Structural diff between two scene or prefab files.
/// </summary>
public sealed class UnitySceneDiff
{
    [JsonPropertyName("added")]
    public IReadOnlyList<string> Added { get; init; } = Array.Empty<string>();

    [JsonPropertyName("removed")]
    public IReadOnlyList<string> Removed { get; init; } = Array.Empty<string>();

    [JsonPropertyName("modified")]
    public IReadOnlyList<string> Modified { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Metadata for a Unity project asset.
/// </summary>
public sealed class UnityAssetMetadata
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("guid")]
    public string? Guid { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("hasMeta")]
    public bool HasMeta { get; init; }

    [JsonPropertyName("dependencies")]
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
}
