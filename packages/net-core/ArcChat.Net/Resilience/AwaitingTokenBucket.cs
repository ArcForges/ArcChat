// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Threading.RateLimiting;

namespace ArcChat.Net.Resilience;

/// <summary>
/// Token-bucket limiter that waits for tokens instead of dropping calls.
/// </summary>
public sealed class AwaitingTokenBucket : IAsyncDisposable
{
    private readonly TokenBucketRateLimiter limiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwaitingTokenBucket"/> class.
    /// </summary>
    public AwaitingTokenBucket(int tokenLimit, int tokensPerPeriod, TimeSpan replenishmentPeriod)
    {
        limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = replenishmentPeriod,
            TokenLimit = tokenLimit,
            TokensPerPeriod = tokensPerPeriod,
        });
    }

    /// <summary>
    /// Waits until the requested number of tokens is available.
    /// </summary>
    public async ValueTask WaitAsync(int tokenCount, CancellationToken cancellationToken = default)
    {
        using RateLimitLease lease = await limiter.AcquireAsync(tokenCount, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Token bucket lease was not acquired.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        limiter.Dispose();
        return ValueTask.CompletedTask;
    }
}
