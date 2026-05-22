// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat ChatMessageTool from app/store/chat.ts.
/// </summary>
/// <param name="Id">Tool call id.</param>
/// <param name="Name">Function name.</param>
/// <param name="Arguments">Serialized function arguments.</param>
/// <param name="Index">Optional streaming index.</param>
/// <param name="Type">Provider tool type.</param>
/// <param name="Content">Tool result content.</param>
/// <param name="IsError">Whether the tool call failed.</param>
/// <param name="ErrorMessage">Tool error message.</param>
public sealed record ChatMessageTool(
    string Id,
    string Name,
    string? Arguments = null,
    int? Index = null,
    string? Type = null,
    string? Content = null,
    bool IsError = false,
    string? ErrorMessage = null);
