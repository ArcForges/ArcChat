// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Composition;
using ArcChat.Desktop.Hosting;
using Microsoft.Extensions.Hosting;

namespace ArcChat.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        _ = builder.Services.AddArcChatDesktop();
        builder.Build().RunAvaloniaApp<App>(args);
    }
}
