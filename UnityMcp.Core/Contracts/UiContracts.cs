using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityMcp.Core.Contracts;

/// <summary>
/// Supported UI control types for the initial UGUI-based pipeline.
/// This enum is intentionally small and extensible.
/// </summary>
public enum UiControlType
{
    Button,
    Label,
    Image,
    Panel,
}

/// <summary>
/// Horizontal / vertical anchoring presets for RectTransform.
/// </summary>
public enum UiAnchorPreset
{
    StretchFull,
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

public sealed class UiColor
{
    [JsonPropertyName("r")]
    public float R { get; init; }

    [JsonPropertyName("g")]
    public float G { get; init; }

    [JsonPropertyName("b")]
    public float B { get; init; }

    [JsonPropertyName("a")]
    public float A { get; init; } = 1f;
}

public sealed class UiVector2
{
    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }
}

/// <summary>
/// Common RectTransform-like layout for UI elements.
/// Coordinates are in anchored pixel space.
/// </summary>
public sealed class UiRectTransform
{
    [JsonPropertyName("anchor")]
    public UiAnchorPreset Anchor { get; init; } = UiAnchorPreset.MiddleCenter;

    [JsonPropertyName("anchoredPosition")]
    public UiVector2 AnchoredPosition { get; init; } = new() { X = 0, Y = 0 };

    [JsonPropertyName("sizeDelta")]
    public UiVector2 SizeDelta { get; init; } = new() { X = 100, Y = 30 };
}

/// <summary>
/// A leaf UI control (e.g. Button, Label, Image) in the layout tree.
/// </summary>
public sealed class UiControl
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; init; } = "Control";

    [JsonPropertyName("type")]
    public UiControlType Type { get; init; } = UiControlType.Button;

    [JsonPropertyName("rect")]
    public UiRectTransform Rect { get; init; } = new();

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("image")]
    public string? ImagePath { get; init; }

    [JsonPropertyName("onClickAction")]
    public string? OnClickAction { get; init; }
}

/// <summary>
/// A logical panel containing child panels and controls.
/// </summary>
public sealed class UiPanel
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; init; } = "Panel";

    [JsonPropertyName("rect")]
    public UiRectTransform Rect { get; init; } = new();

    [JsonPropertyName("children")]
    public IReadOnlyList<UiPanel> Children { get; init; } = Array.Empty<UiPanel>();

    [JsonPropertyName("controls")]
    public IReadOnlyList<UiControl> Controls { get; init; } = Array.Empty<UiControl>();
}

/// <summary>
/// Root UI layout attached to a Canvas.
/// </summary>
public sealed class UiLayout
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "MainMenu";

    [JsonPropertyName("panels")]
    public IReadOnlyList<UiPanel> Panels { get; init; } = Array.Empty<UiPanel>();
}

