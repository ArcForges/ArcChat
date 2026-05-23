// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// In-memory provider registry used by the desktop composition root.
/// </summary>
public sealed class ChatProviderRegistry : IChatProviderRegistry
{
    private readonly ProviderId? fallbackProviderId;
    private readonly Dictionary<string, IChatProvider> providers = new Dictionary<string, IChatProvider>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatProviderRegistry"/> class.
    /// </summary>
    /// <param name="fallbackProviderId">Optional provider id used while concrete NC05 providers are still landing.</param>
    public ChatProviderRegistry(ProviderId? fallbackProviderId = null)
    {
        this.fallbackProviderId = fallbackProviderId;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IChatProvider> Providers => this.providers.Values.ToArray();

    /// <summary>
    /// Adds or replaces a provider registration.
    /// </summary>
    /// <param name="provider">Provider implementation.</param>
    public void Register(IChatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.providers[provider.Id.Value] = provider;
    }

    /// <inheritdoc />
    public IChatProvider Resolve(ProviderId providerId, ModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(providerId.Value))
        {
            throw new ArgumentException("Provider id must not be empty.", nameof(providerId));
        }

        if (this.providers.TryGetValue(providerId.Value, out IChatProvider? provider))
        {
            return provider;
        }

        if (this.fallbackProviderId is ProviderId fallbackProviderId
            && this.providers.TryGetValue(fallbackProviderId.Value, out IChatProvider? fallbackProvider))
        {
            return fallbackProvider;
        }

        throw new KeyNotFoundException($"No chat provider is registered for '{providerId.Value}'.");
    }

    /// <inheritdoc />
    public bool TryResolve(ProviderId providerId, ModelConfig config, out IChatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(providerId.Value))
        {
            throw new ArgumentException("Provider id must not be empty.", nameof(providerId));
        }

        if (this.providers.TryGetValue(providerId.Value, out provider!))
        {
            return true;
        }

        if (this.fallbackProviderId is ProviderId fallbackProviderId
            && this.providers.TryGetValue(fallbackProviderId.Value, out provider!))
        {
            return true;
        }

        provider = null!;
        return false;
    }
}
