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
    /// Lists desktop conversation entries ordered by pin state and user order.
    /// </summary>
    Task<IReadOnlyList<ConversationListEntry>> ListEntriesAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a conversation while keeping the serialized protocol snapshot in sync.
    /// </summary>
    Task RenameAsync(string id, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pins or unpins a conversation.
    /// </summary>
    Task SetPinnedAsync(string id, bool isPinned, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives or unarchives a conversation.
    /// </summary>
    Task SetArchivedAsync(string id, bool isArchived, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the unread count for a conversation.
    /// </summary>
    Task SetUnreadCountAsync(string id, int unreadCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the visible conversation order.
    /// </summary>
    Task ReorderAsync(IReadOnlyList<string> orderedConversationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation by id.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
