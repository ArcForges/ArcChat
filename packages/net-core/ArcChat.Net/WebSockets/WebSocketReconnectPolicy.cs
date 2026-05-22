// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.WebSockets;

/// <summary>
/// Exponential backoff policy for WebSocket reconnects.
/// </summary>
public sealed record WebSocketReconnectPolicy(TimeSpan InitialDelay, TimeSpan MaxDelay)
{
    /// <summary>
    /// Gets the delay for a zero-based reconnect attempt.
    /// </summary>
    public TimeSpan GetDelay(int attempt)
    {
        double factor = Math.Pow(2, Math.Max(0, attempt));
        double milliseconds = Math.Min(this.MaxDelay.TotalMilliseconds, this.InitialDelay.TotalMilliseconds * factor);
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
