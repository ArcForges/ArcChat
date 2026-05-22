// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class SearchChatResult
{
    public SearchChatResult(string conversationId, string conversationTopic, string messageId, MessageRole role, string snippet, int score)
    {
        this.ConversationId = conversationId;
        this.ConversationTopic = conversationTopic;
        this.MessageId = messageId;
        this.Role = role;
        this.Snippet = snippet;
        this.Score = score;
    }

    public string ConversationId { get; }

    public string ConversationTopic { get; }

    public string MessageId { get; }

    public MessageRole Role { get; }

    public string Snippet { get; }

    public int Score { get; }
}
