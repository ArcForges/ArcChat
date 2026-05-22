// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Styling;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Theme.Tests;

public sealed class ThemeSwitchTests
{
    [Theory]
    [InlineData(ColorScheme.System, "Default")]
    [InlineData(ColorScheme.Light, "Light")]
    [InlineData(ColorScheme.Dark, "Dark")]
    public void ThemeSwitchRoundTripsToAvaloniaVariant(ColorScheme colorScheme, string expectedKey)
    {
        Application application = new Application();

        ArcChatTheme.ApplyColorScheme(application, colorScheme);

        string actual = application.RequestedThemeVariant == ThemeVariant.Default
            ? "Default"
            : application.RequestedThemeVariant == ThemeVariant.Light
                ? "Light"
                : "Dark";
        _ = actual.Should().Be(expectedKey);
    }
}
