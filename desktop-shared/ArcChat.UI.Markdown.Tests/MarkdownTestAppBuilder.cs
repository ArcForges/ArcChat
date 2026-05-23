// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia;
using Avalonia.Headless;

namespace ArcChat.UI.Markdown.Tests;

public static class MarkdownTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<MarkdownTestApplication>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            });
    }
}
