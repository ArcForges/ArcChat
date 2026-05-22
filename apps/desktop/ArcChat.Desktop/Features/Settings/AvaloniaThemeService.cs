// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Theme;
using Avalonia;

namespace ArcChat.Desktop.Features.Settings;

internal sealed class AvaloniaThemeService : IThemeService
{
    public void Apply(string theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        ArcChatTheme.ApplyColorScheme(Application.Current, Parse(theme));
    }

    private static ColorScheme Parse(string theme)
    {
        if (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase))
        {
            return ColorScheme.Light;
        }

        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return ColorScheme.Dark;
        }

        return ColorScheme.System;
    }
}
