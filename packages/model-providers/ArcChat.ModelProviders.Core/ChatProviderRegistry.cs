// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// In-memory provider registry used by the desktop composition root.
/// </summary>
public sealed class ChatProviderRegistry : IChatProviderRegistry
{
    private readonly string? fallbackProviderId;
    private readonly Dictionary<string, IChatProvider> providers = new Dictionary<string, IChatProvider>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatProviderRegistry"/> class.
    /// </summary>
    /// <param name="fallbackProviderId">Optional provider id used while concrete NC05 providers are still landing.</param>
    public ChatProviderRegistry(string? fallbackProviderId = null)
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

        this.providers[provider.Id] = provider;
    }

    /// <inheritdoc />
    public IChatProvider Resolve(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        if (this.providers.TryGetValue(providerId, out IChatProvider? provider))
        {
            return provider;
        }

        if (this.fallbackProviderId is not null && this.providers.TryGetValue(this.fallbackProviderId, out IChatProvider? fallbackProvider))
        {
            return fallbackProvider;
        }

        throw new KeyNotFoundException($"No chat provider is registered for '{providerId}'.");
    }

    /// <inheritdoc />
    public bool TryResolve(string providerId, out IChatProvider provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        if (this.providers.TryGetValue(providerId, out provider!))
        {
            return true;
        }

        if (this.fallbackProviderId is not null && this.providers.TryGetValue(this.fallbackProviderId, out provider!))
        {
            return true;
        }

        provider = null!;
        return false;
    }
}
