// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text.Json;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Serialization;

namespace ArcChat.Protocol.Streaming;

/// <summary>
/// Deterministic NDJSON replay helper for recorded provider chat events.
/// </summary>
public static class EventReplay
{
    /// <summary>
    /// Reads a recorded NDJSON stream into typed chat events.
    /// </summary>
    /// <param name="stream">NDJSON stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Typed chat events in recorded order.</returns>
    public static async IAsyncEnumerable<ChatEvent> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new StreamReader(stream, leaveOpen: true);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ChatEvent? chatEvent = JsonSerializer.Deserialize(
                line,
                ArcChatProtocolJsonContext.Default.ChatEvent);
            if (chatEvent is null)
            {
                throw new JsonException("Recorded chat event line deserialized to null.");
            }

            yield return chatEvent;
        }
    }
}
