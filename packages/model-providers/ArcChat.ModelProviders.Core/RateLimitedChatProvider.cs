// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Chat provider decorator that waits for token-bucket permits before streaming.
/// </summary>
public sealed class RateLimitedChatProvider : IChatProvider, IDisposable
{
    private readonly IChatProvider inner;
    private readonly TokenBucketRateLimiter limiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitedChatProvider"/> class.
    /// </summary>
    /// <param name="inner">Provider to decorate.</param>
    /// <param name="limiter">Token bucket used to await provider capacity.</param>
    public RateLimitedChatProvider(IChatProvider inner, TokenBucketRateLimiter limiter)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
    }

    /// <inheritdoc />
    public ProviderId Id => this.inner.Id;

    /// <inheritdoc />
    public ChatProviderCapabilities Capabilities => this.inner.Capabilities;

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using RateLimitLease lease = await this.limiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Token bucket lease was not acquired.");
        }

        await foreach (ChatEvent chatEvent in this.inner.StreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            yield return chatEvent;
        }
    }

    /// <inheritdoc />
    public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return this.inner.ListModelsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.limiter.Dispose();
    }
}
