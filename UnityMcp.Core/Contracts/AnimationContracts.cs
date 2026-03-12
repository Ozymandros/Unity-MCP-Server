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

// ---------------------------------------------------------------------------
// Phase 3 advanced animation and timelines
// ---------------------------------------------------------------------------

/// <summary>
/// Advanced animator layer (Phase 3): can contain states and nested state machines.
/// </summary>
public sealed class AnimatorLayerContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Base Layer";

    [JsonPropertyName("defaultState")]
    public string DefaultState { get; init; } = "Idle";

    [JsonPropertyName("states")]
    public IReadOnlyList<AnimatorStateDef> States { get; init; } = new List<AnimatorStateDef>();

    [JsonPropertyName("subStateMachines")]
    public IReadOnlyList<AnimatorStateMachineContract> SubStateMachines { get; init; } = new List<AnimatorStateMachineContract>();
}

/// <summary>
/// Nested state machine (Phase 3) for locomotion, combat, etc.
/// </summary>
public sealed class AnimatorStateMachineContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("defaultState")]
    public string DefaultState { get; init; } = string.Empty;

    [JsonPropertyName("states")]
    public IReadOnlyList<AnimatorStateDef> States { get; init; } = new List<AnimatorStateDef>();

    [JsonPropertyName("transitions")]
    public IReadOnlyList<AnimatorTransitionDef> Transitions { get; init; } = new List<AnimatorTransitionDef>();
}

/// <summary>
/// Blend tree description (Phase 3). Children typically reference clips or sub-motions.
/// </summary>
public sealed class AnimatorBlendTreeContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "BlendTree";

    [JsonPropertyName("blendParameter")]
    public string BlendParameter { get; init; } = "Speed";

    [JsonPropertyName("children")]
    public IReadOnlyList<AnimatorBlendTreeChild> Children { get; init; } = new List<AnimatorBlendTreeChild>();
}

public sealed class AnimatorBlendTreeChild
{
    [JsonPropertyName("threshold")]
    public float Threshold { get; init; }

    [JsonPropertyName("clip")]
    public string? Clip { get; init; }
}

/// <summary>
/// Timeline root definition (Phase 3): collection of tracks.
/// </summary>
public sealed class TimelineDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Timeline";

    [JsonPropertyName("tracks")]
    public IReadOnlyList<TimelineTrackContract> Tracks { get; init; } = new List<TimelineTrackContract>();
}

/// <summary>
/// Single track within a timeline (animation, audio, etc.).
/// </summary>
public sealed class TimelineTrackContract
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "Animation"; // Animation, Audio, etc.

    [JsonPropertyName("binding")]
    public string Binding { get; init; } = string.Empty;

    [JsonPropertyName("clips")]
    public IReadOnlyList<TimelineClipContract> Clips { get; init; } = new List<TimelineClipContract>();
}

/// <summary>
/// Single clip on a timeline track.
/// </summary>
public sealed class TimelineClipContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("clip")]
    public string? Clip { get; init; } // Animation clip path

    [JsonPropertyName("audio")]
    public string? Audio { get; init; } // Audio clip path

    [JsonPropertyName("start")]
    public float Start { get; init; }

    [JsonPropertyName("duration")]
    public float Duration { get; init; }
}
