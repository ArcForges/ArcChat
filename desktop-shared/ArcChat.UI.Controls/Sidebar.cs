// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace ArcChat.UI.Controls;

/// <summary>
/// NextChat-style sidebar surface used by the desktop shell.
/// </summary>
public class Sidebar : TemplatedControl
{
    /// <summary>Defines the <see cref="Items"/> property.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<Sidebar, IEnumerable?>(nameof(Items));

    /// <summary>Defines the <see cref="SelectedId"/> property.</summary>
    public static readonly StyledProperty<string?> SelectedIdProperty =
        AvaloniaProperty.Register<Sidebar, string?>(nameof(SelectedId));

    /// <summary>Defines the <see cref="IsNarrow"/> property.</summary>
    public static readonly StyledProperty<bool> IsNarrowProperty =
        AvaloniaProperty.Register<Sidebar, bool>(nameof(IsNarrow));

    /// <summary>Defines the <see cref="NavigateCommand"/> property.</summary>
    public static readonly StyledProperty<ICommand?> NavigateCommandProperty =
        AvaloniaProperty.Register<Sidebar, ICommand?>(nameof(NavigateCommand));

    /// <summary>Gets or sets sidebar navigation items.</summary>
    public IEnumerable? Items
    {
        get => this.GetValue(ItemsProperty);
        set => this.SetValue(ItemsProperty, value);
    }

    /// <summary>Gets or sets the selected destination id.</summary>
    public string? SelectedId
    {
        get => this.GetValue(SelectedIdProperty);
        set => this.SetValue(SelectedIdProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the sidebar uses icon-only layout.</summary>
    public bool IsNarrow
    {
        get => this.GetValue(IsNarrowProperty);
        set => this.SetValue(IsNarrowProperty, value);
    }

    /// <summary>Gets or sets the command invoked by sidebar item buttons.</summary>
    public ICommand? NavigateCommand
    {
        get => this.GetValue(NavigateCommandProperty);
        set => this.SetValue(NavigateCommandProperty, value);
    }
}
