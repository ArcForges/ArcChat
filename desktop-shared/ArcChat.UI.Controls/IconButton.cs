// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ArcChat.UI.Controls;

/// <summary>
/// Icon-first command button matching NextChat sidebar and settings affordances.
/// </summary>
public class IconButton : TemplatedControl
{
    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<IconButton, object?>(nameof(Icon));

    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconButton, string?>(nameof(Text));

    /// <summary>Defines the <see cref="IsPrimary"/> property.</summary>
    public static readonly StyledProperty<bool> IsPrimaryProperty =
        AvaloniaProperty.Register<IconButton, bool>(nameof(IsPrimary));

    /// <summary>Defines the <see cref="IsDanger"/> property.</summary>
    public static readonly StyledProperty<bool> IsDangerProperty =
        AvaloniaProperty.Register<IconButton, bool>(nameof(IsDanger));

    /// <summary>Defines the <see cref="IsBordered"/> property.</summary>
    public static readonly StyledProperty<bool> IsBorderedProperty =
        AvaloniaProperty.Register<IconButton, bool>(nameof(IsBordered));

    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<IconButton, object?>(nameof(CommandParameter));

    /// <summary>Defines the click routed event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<IconButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>Raised when the button is invoked.</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => this.AddHandler(ClickEvent, value);
        remove => this.RemoveHandler(ClickEvent, value);
    }

    /// <summary>Gets or sets the icon content.</summary>
    public object? Icon
    {
        get => this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the optional text content.</summary>
    public string? Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the button is a primary command.</summary>
    public bool IsPrimary
    {
        get => this.GetValue(IsPrimaryProperty);
        set => this.SetValue(IsPrimaryProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the button is a destructive command.</summary>
    public bool IsDanger
    {
        get => this.GetValue(IsDangerProperty);
        set => this.SetValue(IsDangerProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the button shows a border.</summary>
    public bool IsBordered
    {
        get => this.GetValue(IsBorderedProperty);
        set => this.SetValue(IsBorderedProperty, value);
    }

    /// <summary>Gets or sets the invocation command.</summary>
    public ICommand? Command
    {
        get => this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    /// <summary>Gets or sets the invocation command parameter.</summary>
    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnApplyTemplate(e);
        this.EnsureAutomationName();

        if (e.NameScope.Find<Button>("PART_Button") is { } button)
        {
            button.Click += this.OnTemplateButtonClicked;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            this.EnsureAutomationName();
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnKeyDown(e);
        if (e.Handled || (e.Key is not Key.Enter && e.Key is not Key.Space))
        {
            return;
        }

        this.Invoke();
        e.Handled = true;
    }

    private void OnTemplateButtonClicked(object? sender, RoutedEventArgs e)
    {
        this.RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    private void Invoke()
    {
        object? parameter = this.CommandParameter;
        if (this.Command?.CanExecute(parameter) == true)
        {
            this.Command.Execute(parameter);
        }

        this.RaiseEvent(new RoutedEventArgs(ClickEvent));
    }

    private void EnsureAutomationName()
    {
        if (!string.IsNullOrWhiteSpace(AutomationProperties.GetName(this)))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(this.Text))
        {
            AutomationProperties.SetName(this, this.Text);
        }
    }
}
