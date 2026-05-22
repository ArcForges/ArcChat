// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Composition;
using Avalonia;
using Microsoft.Extensions.Hosting;

namespace ArcChat.Desktop.Hosting;

internal static class AvaloniaHostExtensions
{
    internal static void RunAvaloniaApp<TApplication>(this IHost host, string[] args)
        where TApplication : Application, new()
    {
        ArgumentNullException.ThrowIfNull(host);

        using (host)
        {
            host.Start();
            DesktopServices.Use(host.Services);
            _ = CreateAppBuilder<TApplication>().StartWithClassicDesktopLifetime(args);
        }
    }

    internal static AppBuilder CreateAppBuilder<TApplication>()
        where TApplication : Application, new()
    {
        AppBuilder builder = AppBuilder.Configure<TApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        return builder;
    }
}
