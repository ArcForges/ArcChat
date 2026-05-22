// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net.WebSockets;

namespace ArcChat.Net.WebSockets;

/// <summary>
/// WebSocket session abstraction used by iFlytek and realtime features.
/// </summary>
public interface IWebSocketSession : IAsyncDisposable
{
    /// <summary>
    /// Sends a UTF-8 text message through the queued sender.
    /// </summary>
    ValueTask SendTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives one WebSocket frame.
    /// </summary>
    ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the session gracefully.
    /// </summary>
    ValueTask CloseAsync(WebSocketCloseStatus status, string? description, CancellationToken cancellationToken = default);
}
