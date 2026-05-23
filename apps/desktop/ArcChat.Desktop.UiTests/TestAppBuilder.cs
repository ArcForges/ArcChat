// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Desktop.UiTests;

public static class TestAppBuilder
{
    public static HeadlessUnitTestSession StartHeadlessSession()
    {
        return HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
    }

    public static void CloseWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.Close();
        Dispatcher.UIThread.RunJobs();
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        ServiceCollection services = new ServiceCollection();
        _ = services.AddArcChatDesktop();
        DesktopServices.Use(services.BuildServiceProvider());

        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            });
    }
}
