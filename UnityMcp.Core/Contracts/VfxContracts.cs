using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// High-level particle effect definition targeting Unity's built-in Particle System.
/// This JSON contract is Unity-agnostic and can be mapped to concrete particle assets later.
/// </summary>
public sealed class ParticleEffectContract
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "ParticleEffect";

    [JsonPropertyName("duration")]
    public float Duration { get; init; } = 5.0f;

    [JsonPropertyName("looping")]
    public bool Looping { get; init; }

    [JsonPropertyName("startLifetime")]
    public float StartLifetime { get; init; } = 1.0f;

    [JsonPropertyName("startSpeed")]
    public float StartSpeed { get; init; } = 5.0f;

    [JsonPropertyName("startSize")]
    public float StartSize { get; init; } = 1.0f;

    [JsonPropertyName("startColor")]
    public ParticleColor StartColor { get; init; } = new();

    [JsonPropertyName("emission")]
    public ParticleEmissionContract Emission { get; init; } = new();

    [JsonPropertyName("shape")]
    public ParticleShapeContract Shape { get; init; } = new();

    /// <summary>
    /// Optional color-over-lifetime definition; may be omitted for simple constant-color effects.
    /// </summary>
    [JsonPropertyName("colorOverLifetime")]
    public ParticleColorOverLifetimeContract? ColorOverLifetime { get; init; }
}

/// <summary>
/// Simple RGBA color used by particle contracts.
/// </summary>
public sealed class ParticleColor
{
    [JsonPropertyName("r")]
    public float R { get; init; } = 1f;

    [JsonPropertyName("g")]
    public float G { get; init; } = 1f;

    [JsonPropertyName("b")]
    public float B { get; init; } = 1f;

    [JsonPropertyName("a")]
    public float A { get; init; } = 1f;
}

/// <summary>
/// Emission configuration: continuous rate and optional burst list.
/// </summary>
public sealed class ParticleEmissionContract
{
    [JsonPropertyName("rateOverTime")]
    public float RateOverTime { get; init; }

    [JsonPropertyName("bursts")]
    public IReadOnlyList<ParticleBurstContract> Bursts { get; init; } = new List<ParticleBurstContract>();
}

/// <summary>
/// Single burst of particles at a specific time.
/// </summary>
public sealed class ParticleBurstContract
{
    [JsonPropertyName("time")]
    public float Time { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }
}

/// <summary>
/// Shape settings for the particle system emitter (sphere, cone, box, etc.).
/// </summary>
public sealed class ParticleShapeContract
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "Sphere";

    [JsonPropertyName("radius")]
    public float Radius { get; init; } = 0.5f;
}

/// <summary>
/// Optional color-over-lifetime gradient expressed as discrete keys.
/// This is forward-looking for richer VFX authoring.
/// </summary>
public sealed class ParticleColorOverLifetimeContract
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("keys")]
    public IReadOnlyList<ParticleColorKey> Keys { get; init; } = new List<ParticleColorKey>();
}

/// <summary>
/// Single color key in a color-over-lifetime gradient.
/// </summary>
public sealed class ParticleColorKey
{
    [JsonPropertyName("time")]
    public float Time { get; init; }

    [JsonPropertyName("color")]
    public ParticleColor Color { get; init; } = new();
}

