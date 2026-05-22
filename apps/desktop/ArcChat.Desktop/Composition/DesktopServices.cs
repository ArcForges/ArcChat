// Copyright (c) ArcForges. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Desktop.Composition;

internal static class DesktopServices
{
    private static IServiceProvider? current;

    internal static void Use(IServiceProvider serviceProvider)
    {
        current = serviceProvider;
    }

    internal static TService GetRequiredService<TService>()
        where TService : notnull
    {
        if (current is null)
        {
            throw new InvalidOperationException("Desktop services have not been initialized.");
        }

        return current.GetRequiredService<TService>();
    }
}
