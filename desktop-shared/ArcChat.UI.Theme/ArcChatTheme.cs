// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace ArcChat.UI.Theme;

/// <summary>
/// Avalonia theme entry point for the NC03 shell, backed by NextChat app/styles/*.scss tokens.
/// </summary>
public sealed class ArcChatTheme : Styles
{
    private ColorScheme colorScheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcChatTheme"/> class.
    /// </summary>
    public ArcChatTheme()
    {
        Uri baseUri = new Uri("avares://ArcChat.UI.Theme/");
        this.Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri("avares://ArcChat.UI.Theme/Tokens/Colors.axaml"),
        });
        this.Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri("avares://ArcChat.UI.Theme/Tokens/Typography.axaml"),
        });
        this.Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri("avares://ArcChat.UI.Theme/Tokens/Shape.axaml"),
        });
        this.Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri("avares://ArcChat.UI.Theme/Tokens/Motion.axaml"),
        });
        this.Add(new FluentTheme());
    }

    /// <summary>
    /// Gets or sets the active color scheme.
    /// </summary>
    public ColorScheme ColorScheme
    {
        get => this.colorScheme;
        set
        {
            this.colorScheme = value;
            if (Application.Current is { } application)
            {
                ApplyColorScheme(application, value);
            }
        }
    }

    /// <summary>
    /// Applies an ArcChat color scheme to the provided Avalonia application.
    /// </summary>
    /// <param name="application">Avalonia application receiving the theme variant.</param>
    /// <param name="scheme">ArcChat color scheme.</param>
    public static void ApplyColorScheme(Application application, ColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.RequestedThemeVariant = scheme switch
        {
            ColorScheme.Light => ThemeVariant.Light,
            ColorScheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
