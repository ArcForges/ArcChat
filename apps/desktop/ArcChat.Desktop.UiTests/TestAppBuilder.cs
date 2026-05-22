// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Composition;
using Avalonia;
using Avalonia.Headless;
using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Desktop.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        ServiceCollection services = new ServiceCollection();
        _ = services.AddArcChatDesktop();
        DesktopServices.Use(services.BuildServiceProvider());

        return AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
