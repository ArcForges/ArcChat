// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Sse;

/// <summary>
/// Server-sent event parsed from an EventSource stream.
/// </summary>
public sealed record SseEvent(string? Id, string? Event, string Data, int? RetryMs);
