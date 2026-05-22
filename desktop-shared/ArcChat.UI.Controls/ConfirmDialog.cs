// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ArcChat.UI.Controls;

/// <summary>
/// Reusable confirmation dialog primitive for destructive shell actions.
/// </summary>
public class ConfirmDialog : TemplatedControl
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ConfirmDialog, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Message"/> property.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialog, string?>(nameof(Message));

    /// <summary>Defines the <see cref="ConfirmText"/> property.</summary>
    public static readonly StyledProperty<string> ConfirmTextProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(ConfirmText), "Confirm");

    /// <summary>Defines the <see cref="CancelText"/> property.</summary>
    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(CancelText), "Cancel");

    /// <summary>Defines the <see cref="ConfirmCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand?>(nameof(ConfirmCommand));

    /// <summary>Defines the <see cref="CancelCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ConfirmDialog, ICommand?>(nameof(CancelCommand));

    /// <summary>Defines the confirmed routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ConfirmedEvent =
        RoutedEvent.Register<ConfirmDialog, RoutedEventArgs>(nameof(Confirmed), RoutingStrategies.Bubble);

    /// <summary>Defines the cancelled routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent =
        RoutedEvent.Register<ConfirmDialog, RoutedEventArgs>(nameof(Cancelled), RoutingStrategies.Bubble);

    /// <summary>Raised when the confirm action is invoked.</summary>
    public event EventHandler<RoutedEventArgs>? Confirmed
    {
        add => this.AddHandler(ConfirmedEvent, value);
        remove => this.RemoveHandler(ConfirmedEvent, value);
    }

    /// <summary>Raised when the cancel action is invoked.</summary>
    public event EventHandler<RoutedEventArgs>? Cancelled
    {
        add => this.AddHandler(CancelledEvent, value);
        remove => this.RemoveHandler(CancelledEvent, value);
    }

    /// <summary>Gets or sets dialog title text.</summary>
    public string? Title
    {
        get => this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets dialog body text.</summary>
    public string? Message
    {
        get => this.GetValue(MessageProperty);
        set => this.SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets confirm button text.</summary>
    public string ConfirmText
    {
        get => this.GetValue(ConfirmTextProperty);
        set => this.SetValue(ConfirmTextProperty, value);
    }

    /// <summary>Gets or sets cancel button text.</summary>
    public string CancelText
    {
        get => this.GetValue(CancelTextProperty);
        set => this.SetValue(CancelTextProperty, value);
    }

    /// <summary>Gets or sets confirm command.</summary>
    public ICommand? ConfirmCommand
    {
        get => this.GetValue(ConfirmCommandProperty);
        set => this.SetValue(ConfirmCommandProperty, value);
    }

    /// <summary>Gets or sets cancel command.</summary>
    public ICommand? CancelCommand
    {
        get => this.GetValue(CancelCommandProperty);
        set => this.SetValue(CancelCommandProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Button>("PART_ConfirmButton") is { } confirm)
        {
            confirm.Click += this.OnConfirmClicked;
        }

        if (e.NameScope.Find<Button>("PART_CancelButton") is { } cancel)
        {
            cancel.Click += this.OnCancelClicked;
        }
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (this.ConfirmCommand?.CanExecute(null) == true)
        {
            this.ConfirmCommand.Execute(null);
        }

        this.RaiseEvent(new RoutedEventArgs(ConfirmedEvent));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        if (this.CancelCommand?.CanExecute(null) == true)
        {
            this.CancelCommand.Execute(null);
        }

        this.RaiseEvent(new RoutedEventArgs(CancelledEvent));
    }
}
