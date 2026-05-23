// Copyright (c) ArcForges. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Dependency injection registration for chat provider foundations.
/// </summary>
public static class ChatProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds ArcChat chat provider registry services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddArcChatProviders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<IChatProviderRegistry>(provider =>
        {
            ChatProviderRegistry registry = ModelProviderCoreDefaults.CreateRegistry();
            foreach (IChatProvider chatProvider in provider.GetServices<IChatProvider>())
            {
                registry.Register(chatProvider);
            }

            return registry;
        });
        return services;
    }
}
