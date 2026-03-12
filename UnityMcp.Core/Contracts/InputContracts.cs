using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Root Input Actions asset (e.g. PlayerControls). Maps to Unity Input System .inputactions.
/// </summary>
public sealed class InputActionsAsset
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "PlayerControls";

    [JsonPropertyName("maps")]
    public IReadOnlyList<InputActionMapDef> Maps { get; init; } = new List<InputActionMapDef>();
}

/// <summary>
/// One action map (e.g. Gameplay) with actions and bindings.
/// </summary>
public sealed class InputActionMapDef
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Gameplay";

    [JsonPropertyName("actions")]
    public IReadOnlyList<InputActionDef> Actions { get; init; } = new List<InputActionDef>();

    [JsonPropertyName("bindings")]
    public IReadOnlyList<InputBindingDef> Bindings { get; init; } = new List<InputBindingDef>();
}

/// <summary>
/// Single action (e.g. Move, Jump) with type and expected control type.
/// </summary>
public sealed class InputActionDef
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "Button"; // Button, Value, PassThrough

    [JsonPropertyName("expectedControlType")]
    public string ExpectedControlType { get; init; } = "Button"; // Button, Vector2, etc.
}

/// <summary>
/// Binding of an action to a control path.
/// </summary>
public sealed class InputBindingDef
{
    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("interactions")]
    public string? Interactions { get; init; }

    [JsonPropertyName("processors")]
    public string? Processors { get; init; }
}
