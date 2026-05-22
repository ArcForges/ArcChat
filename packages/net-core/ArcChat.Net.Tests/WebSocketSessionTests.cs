// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net.WebSockets;
using System.Text;
using ArcChat.Net.WebSockets;
using FluentAssertions;
using Xunit;

namespace ArcChat.Net.Tests;

public sealed class WebSocketSessionTests
{
    [Fact]
    public async Task SendTextAsyncQueuesUtf8Message()
    {
        using RecordingWebSocket socket = new();
        await using WebSocketSession session = new(socket, TimeSpan.Zero);

        await session.SendTextAsync("hello", CancellationToken.None);

        await socket.SentSignal.Task.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        _ = socket.SentMessages.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task CloseAsyncCompletesOpenSocket()
    {
        using RecordingWebSocket socket = new();
        await using WebSocketSession session = new(socket, TimeSpan.Zero);

        await session.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        _ = socket.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
        _ = socket.CloseStatusDescription.Should().Be("done");
    }

    [Fact]
    public void ReconnectPolicyCapsExponentialDelay()
    {
        WebSocketReconnectPolicy policy = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        _ = policy.GetDelay(0).Should().Be(TimeSpan.FromSeconds(1));
        _ = policy.GetDelay(2).Should().Be(TimeSpan.FromSeconds(4));
        _ = policy.GetDelay(4).Should().Be(TimeSpan.FromSeconds(5));
    }

    private sealed class RecordingWebSocket : WebSocket
    {
        private WebSocketCloseStatus? closeStatus;
        private string? closeDescription;

        public List<string> SentMessages { get; } = new List<string>();

        public TaskCompletionSource SentSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override WebSocketCloseStatus? CloseStatus => closeStatus;

        public override string? CloseStatusDescription => closeDescription;

        public override WebSocketState State { get; } = WebSocketState.Open;

        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            this.closeStatus = closeStatus;
            closeDescription = statusDescription;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            this.closeStatus = closeStatus;
            closeDescription = statusDescription;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            SentMessages.Add(Encoding.UTF8.GetString(buffer));
            SentSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public override ValueTask SendAsync(
            ReadOnlyMemory<byte> buffer,
            WebSocketMessageType messageType,
            WebSocketMessageFlags messageFlags,
            CancellationToken cancellationToken)
        {
            SentMessages.Add(Encoding.UTF8.GetString(buffer.Span));
            SentSignal.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }
}
