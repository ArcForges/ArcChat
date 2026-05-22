// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace ArcChat.Net.Sse;

/// <summary>
/// Deterministic SSE reader over a PipeReader-backed stream.
/// </summary>
public sealed class ServerSentEventReader
{
    /// <summary>
    /// Reads events from a server-sent event stream.
    /// </summary>
    public async IAsyncEnumerable<SseEvent> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        PipeReader pipeReader = PipeReader.Create(stream);
        await using Stream readerStream = pipeReader.AsStream(leaveOpen: false);
        using StreamReader reader = new StreamReader(readerStream);
        SseEventBuilder builder = new SseEventBuilder();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (builder.TryBuild(out SseEvent? sseEvent))
                {
                    yield return sseEvent;
                }

                builder = new SseEventBuilder();
                continue;
            }

            builder.AppendLine(line);
        }

        if (builder.TryBuild(out SseEvent? finalEvent))
        {
            yield return finalEvent;
        }
    }

    private sealed class SseEventBuilder
    {
        private readonly List<string> dataLines = new List<string>();
        private string? id;
        private string? eventName;
        private int? retryMs;

        public void AppendLine(string line)
        {
            if (line.StartsWith(':'))
            {
                return;
            }

            int separator = line.IndexOf(':', StringComparison.Ordinal);
            string field = separator < 0 ? line : line[..separator];
            string value = separator < 0 ? string.Empty : line[(separator + 1)..].TrimStart(' ');

            switch (field)
            {
                case "id":
                    id = value;
                    break;
                case "event":
                    eventName = value;
                    break;
                case "data":
                    dataLines.Add(value);
                    break;
                case "retry" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedRetry):
                    retryMs = parsedRetry;
                    break;
            }
        }

        public bool TryBuild([NotNullWhen(true)] out SseEvent? sseEvent)
        {
            if (dataLines.Count == 0 && id is null && eventName is null && retryMs is null)
            {
                sseEvent = null;
                return false;
            }

            sseEvent = new SseEvent(this.id, this.eventName, string.Join('\n', this.dataLines), this.retryMs);
            return true;
        }
    }
}
