// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Resolves configured chat providers by NextChat provider id and model config.
/// </summary>
public interface IChatProviderRegistry
{
    /// <summary>
    /// Gets the currently registered providers.
    /// </summary>
    IReadOnlyCollection<IChatProvider> Providers { get; }

    /// <summary>
    /// Resolves a provider or throws when none is registered.
    /// </summary>
    /// <param name="providerId">Provider id from <c>ModelConfig.ProviderName</c>.</param>
    /// <param name="config">Selected model config used for compatibility and fallback resolution.</param>
    /// <returns>The matching provider.</returns>
    IChatProvider Resolve(ProviderId providerId, ModelConfig config);

    /// <summary>
    /// Attempts to resolve a provider.
    /// </summary>
    /// <param name="providerId">Provider id from <c>ModelConfig.ProviderName</c>.</param>
    /// <param name="config">Selected model config used for compatibility and fallback resolution.</param>
    /// <param name="provider">Resolved provider when available.</param>
    /// <returns><see langword="true"/> when a provider was resolved.</returns>
    bool TryResolve(ProviderId providerId, ModelConfig config, out IChatProvider provider);
}
