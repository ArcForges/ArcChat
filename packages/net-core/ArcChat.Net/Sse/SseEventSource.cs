// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net;
using System.Runtime.CompilerServices;

namespace ArcChat.Net.Sse;

/// <summary>
/// Reconnecting SSE source that preserves Last-Event-ID.
/// </summary>
public sealed class SseEventSource
{
    private readonly ServerSentEventReader reader;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> delayAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="SseEventSource"/> class.
    /// </summary>
    public SseEventSource(
        ServerSentEventReader reader,
        Func<TimeSpan, CancellationToken, ValueTask>? delayAsync = null)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.delayAsync = delayAsync ?? DelayAsync;
    }

    /// <summary>
    /// Reads SSE events and retries transient 5xx responses with Last-Event-ID.
    /// </summary>
    public async IAsyncEnumerable<SseEvent> ReadWithReconnectAsync(
        Func<string?, CancellationToken, Task<HttpResponseMessage>> openAsync,
        int maxRetries,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openAsync);

        string? lastEventId = null;
        int attempt = 0;

        while (true)
        {
            using HttpResponseMessage response = await openAsync(lastEventId, cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode >= 500 && attempt < maxRetries)
            {
                attempt++;
                TimeSpan delay = RetryAfterDelay.GetDelay(response.Headers);
                await this.delayAsync(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests && attempt < maxRetries)
            {
                attempt++;
                TimeSpan delay = RetryAfterDelay.GetDelay(response.Headers);
                await this.delayAsync(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            _ = response.EnsureSuccessStatusCode();
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await foreach (SseEvent sseEvent in this.reader.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(sseEvent.Id))
                {
                    lastEventId = sseEvent.Id;
                }

                yield return sseEvent;
            }

            yield break;
        }
    }

    private static async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}
