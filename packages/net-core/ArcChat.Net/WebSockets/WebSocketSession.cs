// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace ArcChat.Net.WebSockets;

/// <summary>
/// Queued WebSocket session with optional heartbeat.
/// </summary>
public sealed class WebSocketSession : IWebSocketSession
{
    private readonly WebSocket socket;
    private readonly Channel<QueuedMessage> sendQueue = Channel.CreateUnbounded<QueuedMessage>();
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private readonly Task sendPump;
    private readonly Task heartbeatPump;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketSession"/> class.
    /// </summary>
    public WebSocketSession(WebSocket socket, TimeSpan heartbeatInterval)
    {
        this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        this.sendPump = Task.Run(this.SendPumpAsync);
        this.heartbeatPump = heartbeatInterval <= TimeSpan.Zero
            ? Task.CompletedTask
            : Task.Run(() => this.HeartbeatPumpAsync(heartbeatInterval));
    }

    /// <summary>
    /// Connects a ClientWebSocket and wraps it in a session.
    /// </summary>
    public static async Task<WebSocketSession> ConnectAsync(
        Uri uri,
        TimeSpan heartbeatInterval,
        CancellationToken cancellationToken = default)
    {
        ClientWebSocket clientWebSocket = new ClientWebSocket();
        await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        return new WebSocketSession(clientWebSocket, heartbeatInterval);
    }

    /// <inheritdoc />
    public async ValueTask SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        byte[] payload = Encoding.UTF8.GetBytes(text);
        await this.sendQueue.Writer.WriteAsync(new QueuedMessage(payload, WebSocketMessageType.Text), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return await this.socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken cancellationToken = default)
    {
        _ = this.sendQueue.Writer.TryComplete();
        if (this.socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await this.socket.CloseAsync(status, description, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await this.lifetime.CancelAsync().ConfigureAwait(false);
        _ = this.sendQueue.Writer.TryComplete();
        try
        {
            await Task.WhenAll(this.sendPump, this.heartbeatPump).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        this.socket.Dispose();
        this.lifetime.Dispose();
    }

    private async Task SendPumpAsync()
    {
        await foreach (QueuedMessage message in this.sendQueue.Reader.ReadAllAsync(this.lifetime.Token).ConfigureAwait(false))
        {
            await this.socket.SendAsync(message.Payload, message.Type, true, this.lifetime.Token).ConfigureAwait(false);
        }
    }

    private async Task HeartbeatPumpAsync(TimeSpan interval)
    {
        using PeriodicTimer timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(this.lifetime.Token).ConfigureAwait(false))
        {
            await this.SendTextAsync("{\"type\":\"ping\"}", this.lifetime.Token).ConfigureAwait(false);
        }
    }

    private sealed record QueuedMessage(byte[] Payload, WebSocketMessageType Type);
}
