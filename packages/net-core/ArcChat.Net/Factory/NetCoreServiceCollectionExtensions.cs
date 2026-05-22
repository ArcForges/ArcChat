// Copyright (c) ArcForges. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Net.Factory;

/// <summary>
/// Dependency injection registration for ArcChat.Net.
/// </summary>
public static class NetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds NC02 ArcChat.Net services and named HTTP profiles.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddArcChatNetCore(this IServiceCollection services)
    {
        _ = services.AddOptions<NetCoreFactoryOptions>();
        NetCoreFactoryOptions defaults = new NetCoreFactoryOptions();

        foreach (NetClientProfile profile in defaults.Profiles)
        {
            _ = services.AddHttpClient(profile.Name, client => client.Timeout = profile.Timeout)
                .AddStandardResilienceHandler();
        }

        _ = services.AddSingleton<INetCoreFactory, NetCoreFactory>();
        return services;
    }
}
