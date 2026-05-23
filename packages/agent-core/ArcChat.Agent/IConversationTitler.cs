// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.Agent;

/// <summary>
/// Generates a short display title for a conversation.
/// </summary>
public interface IConversationTitler
{
    /// <summary>
    /// Generates a title from the first completed conversation exchange.
    /// </summary>
    /// <param name="conversation">Conversation to title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trimmed title text, or an empty string when no title is produced.</returns>
    Task<string> GenerateTitleAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
