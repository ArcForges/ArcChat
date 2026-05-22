// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Headless;

namespace ArcChat.UI.Controls.Tests;

public static class ControlTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<ControlTestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
