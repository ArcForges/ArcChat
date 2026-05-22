// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net.Http.Headers;

namespace ArcChat.Net.Sse;

/// <summary>
/// Parses Retry-After values from provider HTTP responses.
/// </summary>
public static class RetryAfterDelay
{
    /// <summary>
    /// Gets a retry delay from a Retry-After header.
    /// </summary>
    public static TimeSpan GetDelay(HttpResponseHeaders headers, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (headers.RetryAfter is null)
        {
            return TimeSpan.Zero;
        }

        if (headers.RetryAfter.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (headers.RetryAfter.Date is { } date)
        {
            DateTimeOffset now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            TimeSpan delay = date - now;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return TimeSpan.Zero;
    }
}
