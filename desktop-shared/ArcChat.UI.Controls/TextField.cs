// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ArcChat.UI.Controls;

/// <summary>
/// Styled text input wrapper for shell and settings surfaces.
/// </summary>
public class TextField : TemplatedControl
{
    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines the <see cref="Watermark"/> property.</summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<TextField, string?>(nameof(Watermark));

    /// <summary>Defines the <see cref="AcceptsReturn"/> property.</summary>
    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<TextField, bool>(nameof(AcceptsReturn));

    /// <summary>Defines the text changed routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> TextChangedEvent =
        RoutedEvent.Register<TextField, RoutedEventArgs>(nameof(TextChanged), RoutingStrategies.Bubble);

    /// <summary>Raised when text changes.</summary>
    public event EventHandler<RoutedEventArgs>? TextChanged
    {
        add => this.AddHandler(TextChangedEvent, value);
        remove => this.RemoveHandler(TextChangedEvent, value);
    }

    /// <summary>Gets or sets the field text.</summary>
    public string? Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets placeholder text.</summary>
    public string? Watermark
    {
        get => this.GetValue(WatermarkProperty);
        set => this.SetValue(WatermarkProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the field accepts line breaks.</summary>
    public bool AcceptsReturn
    {
        get => this.GetValue(AcceptsReturnProperty);
        set => this.SetValue(AcceptsReturnProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            this.RaiseEvent(new RoutedEventArgs(TextChangedEvent));
        }
    }
}
