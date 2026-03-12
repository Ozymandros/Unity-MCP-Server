using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Single bone in a ragdoll or physics setup.
/// </summary>
public sealed class RagdollBoneContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Collider shape used for this bone (e.g. "Capsule", "Box", "Sphere").
    /// </summary>
    [JsonPropertyName("colliderType")]
    public string ColliderType { get; init; } = "Capsule";

    /// <summary>
    /// Mass assigned to this bone in kilograms.
    /// </summary>
    [JsonPropertyName("mass")]
    public float Mass { get; init; } = 1.0f;

    /// <summary>
    /// Optional collider dimensions (radius, height, width, depth, etc.).
    /// Schema is intentionally flexible for future expansion.
    /// </summary>
    [JsonPropertyName("dimensions")]
    public IDictionary<string, float>? Dimensions { get; init; }
}

/// <summary>
/// Joint definition connecting two bones in a ragdoll or physics rig.
/// </summary>
public sealed class JointContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Joint type (e.g. "Hinge", "Configurable", "Fixed").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "Hinge";

    /// <summary>
    /// Name of the primary bone this joint is attached to.
    /// </summary>
    [JsonPropertyName("bone")]
    public string Bone { get; init; } = string.Empty;

    /// <summary>
    /// Name of the connected body/bone, if any.
    /// </summary>
    [JsonPropertyName("connectedBodyName")]
    public string? ConnectedBodyName { get; init; }
}

/// <summary>
/// Root physics setup for a ragdoll or joint rig.
/// </summary>
public sealed class RagdollSetupContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "PhysicsSetup";

    [JsonPropertyName("bones")]
    public IReadOnlyList<RagdollBoneContract> Bones { get; init; } = new List<RagdollBoneContract>();

    [JsonPropertyName("joints")]
    public IReadOnlyList<JointContract> Joints { get; init; } = new List<JointContract>();
}

