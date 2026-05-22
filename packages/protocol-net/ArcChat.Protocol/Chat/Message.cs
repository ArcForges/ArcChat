// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat ChatMessage mapped from app/store/chat.ts.
/// </summary>
/// <param name="Id">Stable message id.</param>
/// <param name="Role">Message role.</param>
/// <param name="Content">Text, image, and tool content blocks.</param>
/// <param name="Date">NextChat-compatible display date value.</param>
/// <param name="Streaming">Whether the message is still streaming.</param>
/// <param name="IsError">Whether the message represents an error.</param>
/// <param name="Model">Model id used for assistant output.</param>
/// <param name="Tools">Provider tool calls attached to the message.</param>
/// <param name="AudioUrl">Realtime audio metadata from NextChat audio_url.</param>
/// <param name="IsMcpResponse">Whether this message is a NextChat MCP response block.</param>
public sealed record Message(
    string Id,
    MessageRole Role,
    ImmutableArray<ContentBlock> Content,
    string Date,
    bool Streaming = false,
    bool IsError = false,
    string? Model = null,
    ImmutableArray<ChatMessageTool> Tools = default,
    string? AudioUrl = null,
    bool IsMcpResponse = false)
{
    /// <summary>
    /// Creates a plain text message with a NextChat role.
    /// </summary>
    /// <param name="id">Stable message id.</param>
    /// <param name="role">Message role.</param>
    /// <param name="text">Text content.</param>
    /// <param name="date">Display date value.</param>
    /// <returns>A message containing one text block.</returns>
    public static Message Text(string id, MessageRole role, string text, string date)
    {
        return new Message(
            id,
            role,
            ImmutableArray.Create<ContentBlock>(new TextBlock(text)),
            date,
            Tools: ImmutableArray<ChatMessageTool>.Empty);
    }
}
