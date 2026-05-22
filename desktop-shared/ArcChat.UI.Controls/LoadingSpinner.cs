// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls.Primitives;

namespace ArcChat.UI.Controls;

/// <summary>
/// Deterministic loading primitive for shell and dialog placeholders.
/// </summary>
public class LoadingSpinner : TemplatedControl
{
    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<LoadingSpinner, string?>(nameof(Message));

    /// <summary>Gets or sets accessible loading message.</summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }
}
