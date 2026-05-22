// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ArcChat.UI.Controls;

/// <summary>
/// Two-pane layout primitive with a persisted splitter length.
/// </summary>
public class ResizableSplitPane : TemplatedControl
{
    /// <summary>Defines the <see cref="PaneLength"/> property.</summary>
    public static readonly StyledProperty<GridLength> PaneLengthProperty =
        AvaloniaProperty.Register<ResizableSplitPane, GridLength>(nameof(PaneLength), new GridLength(300));

    /// <summary>Defines the <see cref="MinPaneLength"/> property.</summary>
    public static readonly StyledProperty<double> MinPaneLengthProperty =
        AvaloniaProperty.Register<ResizableSplitPane, double>(nameof(MinPaneLength), 100);

    /// <summary>Defines the <see cref="MaxPaneLength"/> property.</summary>
    public static readonly StyledProperty<double> MaxPaneLengthProperty =
        AvaloniaProperty.Register<ResizableSplitPane, double>(nameof(MaxPaneLength), 500);

    /// <summary>Defines the <see cref="Left"/> property.</summary>
    public static readonly StyledProperty<object?> LeftProperty =
        AvaloniaProperty.Register<ResizableSplitPane, object?>(nameof(Left));

    /// <summary>Defines the <see cref="Right"/> property.</summary>
    public static readonly StyledProperty<object?> RightProperty =
        AvaloniaProperty.Register<ResizableSplitPane, object?>(nameof(Right));

    /// <summary>Defines the pane length changed routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> PaneLengthChangedEvent =
        RoutedEvent.Register<ResizableSplitPane, RoutedEventArgs>(nameof(PaneLengthChanged), RoutingStrategies.Bubble);

    /// <summary>Raised when the leading pane length changes.</summary>
    public event EventHandler<RoutedEventArgs>? PaneLengthChanged
    {
        add => this.AddHandler(PaneLengthChangedEvent, value);
        remove => this.RemoveHandler(PaneLengthChangedEvent, value);
    }

    /// <summary>Gets or sets the leading pane width.</summary>
    public GridLength PaneLength
    {
        get => this.GetValue(PaneLengthProperty);
        set => this.SetValue(PaneLengthProperty, value);
    }

    /// <summary>Gets or sets the minimum leading pane width.</summary>
    public double MinPaneLength
    {
        get => this.GetValue(MinPaneLengthProperty);
        set => this.SetValue(MinPaneLengthProperty, value);
    }

    /// <summary>Gets or sets the maximum leading pane width.</summary>
    public double MaxPaneLength
    {
        get => this.GetValue(MaxPaneLengthProperty);
        set => this.SetValue(MaxPaneLengthProperty, value);
    }

    /// <summary>Gets or sets content for the leading pane.</summary>
    public object? Left
    {
        get => this.GetValue(LeftProperty);
        set => this.SetValue(LeftProperty, value);
    }

    /// <summary>Gets or sets content for the trailing pane.</summary>
    public object? Right
    {
        get => this.GetValue(RightProperty);
        set => this.SetValue(RightProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == PaneLengthProperty)
        {
            this.RaiseEvent(new RoutedEventArgs(PaneLengthChangedEvent));
        }
    }
}
