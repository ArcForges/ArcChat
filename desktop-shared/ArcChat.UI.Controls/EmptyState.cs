// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace ArcChat.UI.Controls;

/// <summary>
/// Empty-state panel used by NC03 shell placeholders.
/// </summary>
public class EmptyState : TemplatedControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));

    /// <summary>Defines the <see cref="ActionText"/> property.</summary>
    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(ActionText));

    /// <summary>Defines the <see cref="ActionCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<EmptyState, ICommand?>(nameof(ActionCommand));

    /// <summary>Gets or sets title text.</summary>
    public string? Title
    {
        get => this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets body text.</summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets optional action text.</summary>
    public string? ActionText
    {
        get => this.GetValue(ActionTextProperty);
        set => this.SetValue(ActionTextProperty, value);
    }

    /// <summary>Gets or sets optional action command.</summary>
    public ICommand? ActionCommand
    {
        get => this.GetValue(ActionCommandProperty);
        set => this.SetValue(ActionCommandProperty, value);
    }
}
