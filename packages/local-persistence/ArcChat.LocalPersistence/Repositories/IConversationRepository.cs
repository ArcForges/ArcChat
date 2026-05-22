// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.LocalPersistence.Repositories;

/// <summary>
/// Stores ArcChat conversations.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Inserts or updates a conversation.
    /// </summary>
    Task UpsertAsync(Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation by id.
    /// </summary>
    Task<Conversation?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists conversations ordered by most recent update.
    /// </summary>
    Task<IReadOnlyList<Conversation>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation by id.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
