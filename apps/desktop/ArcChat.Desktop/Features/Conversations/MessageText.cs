// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ProtocolTextBlock = ArcChat.Protocol.Chat.TextBlock;

namespace ArcChat.Desktop.Features.Conversations;

internal static class MessageText
{
    public static string Extract(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return string.Concat(message.Content.OfType<ProtocolTextBlock>().Select(block => block.Text));
    }
}
