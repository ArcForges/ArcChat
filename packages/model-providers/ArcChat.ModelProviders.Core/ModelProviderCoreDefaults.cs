// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.ModelProviders.Core.Internal;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Creates provider-core defaults used before concrete network providers land.
/// </summary>
public static class ModelProviderCoreDefaults
{
    /// <summary>
    /// Gets the deterministic echo provider id.
    /// </summary>
    public const string EchoProviderId = "Echo";

    /// <summary>
    /// Creates the default registry with the local echo provider as fallback.
    /// </summary>
    /// <returns>A registry containing the deterministic echo provider.</returns>
    public static ChatProviderRegistry CreateRegistry()
    {
        ChatProviderRegistry registry = new ChatProviderRegistry(EchoProviderId);
        registry.Register(new EchoProvider(EchoProviderId));
        return registry;
    }
}
