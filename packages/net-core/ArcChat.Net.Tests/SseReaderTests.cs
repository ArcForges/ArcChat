// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Text;
using ArcChat.Net.Sse;
using FluentAssertions;
using Xunit;

namespace ArcChat.Net.Tests;

public sealed class SseReaderTests
{
    [Fact]
    public async Task ReaderParsesRecordedStreamInOrder()
    {
        const string Body = "id: 1\nevent: delta\nretry: 25\ndata: {\"delta\":\"Hello\"}\n\nid: 2\ndata: [DONE]\n\n";
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes(Body));
        ServerSentEventReader reader = new();

        List<SseEvent> events = new();
        await foreach (SseEvent sseEvent in reader.ReadAsync(stream, CancellationToken.None))
        {
            events.Add(sseEvent);
        }

        _ = events.Should().HaveCount(2);
        _ = events[0].Id.Should().Be("1");
        _ = events[0].Event.Should().Be("delta");
        _ = events[0].RetryMs.Should().Be(25);
        _ = events[1].Data.Should().Be("[DONE]");
    }

    [Fact]
    public async Task ReaderHonorsCancellationWithinAbortBudget()
    {
        await using NeverEndingStream stream = new();
        ServerSentEventReader reader = new();
        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));
        Stopwatch stopwatch = Stopwatch.StartNew();

        Func<Task> read = async () =>
        {
            await foreach (SseEvent _ in reader.ReadAsync(stream, cts.Token))
            {
            }
        };

        await read.Should().ThrowAsync<OperationCanceledException>();
        _ = stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task ReconnectRetriesTransientResponseAndSendsLastEventId()
    {
        List<string?> lastEventIds = new();
        List<TimeSpan> delays = new();
        SseEventSource source = new(
            new ServerSentEventReader(),
            (delay, _) =>
            {
                delays.Add(delay);
                return ValueTask.CompletedTask;
            });

        int call = 0;
        async Task<HttpResponseMessage> OpenAsync(string? lastEventId, CancellationToken _)
        {
            lastEventIds.Add(lastEventId);
            call++;
            if (call == 1)
            {
                HttpResponseMessage retry = new(HttpStatusCode.ServiceUnavailable);
                retry.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
                return retry;
            }

            HttpResponseMessage ok = new(HttpStatusCode.OK)
            {
                Content = new StringContent("id: abc\ndata: done\n\n", Encoding.UTF8, "text/event-stream"),
            };
            return await Task.FromResult(ok);
        }

        List<SseEvent> events = new();
        await foreach (SseEvent sseEvent in source.ReadWithReconnectAsync(OpenAsync, 2, CancellationToken.None))
        {
            events.Add(sseEvent);
        }

        _ = lastEventIds.Should().Equal(null, null);
        _ = delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(2));
        _ = events.Should().ContainSingle().Which.Id.Should().Be("abc");
    }

    private sealed class NeverEndingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
