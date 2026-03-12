using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Basic Animator Controller definition (Phase 2: single layer, simple transitions).
/// No blend trees or sub-state machines.
/// </summary>
public sealed class BasicAnimatorDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Animator";

    [JsonPropertyName("defaultState")]
    public string DefaultState { get; init; } = "Idle";

    [JsonPropertyName("states")]
    public IReadOnlyList<AnimatorStateDef> States { get; init; } = new List<AnimatorStateDef>();

    [JsonPropertyName("transitions")]
    public IReadOnlyList<AnimatorTransitionDef> Transitions { get; init; } = new List<AnimatorTransitionDef>();
}

/// <summary>
/// Single state with optional animation clip path.
/// </summary>
public sealed class AnimatorStateDef
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("clip")]
    public string? Clip { get; init; } // e.g. Assets/Animations/Idle.anim
}

/// <summary>
/// Transition between two states with a condition (e.g. "Speed > 0.1").
/// Phase 2: simple bool/float parameter conditions only.
/// </summary>
public sealed class AnimatorTransitionDef
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; init; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; init; } = string.Empty;
}
