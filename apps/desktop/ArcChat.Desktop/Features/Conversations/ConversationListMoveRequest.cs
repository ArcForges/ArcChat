// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal sealed record ConversationListMoveRequest
{
    public ConversationListMoveRequest(string conversationId, int targetIndex)
    {
        this.ConversationId = conversationId;
        this.TargetIndex = targetIndex;
    }

    public string ConversationId { get; }

    public int TargetIndex { get; }
}
