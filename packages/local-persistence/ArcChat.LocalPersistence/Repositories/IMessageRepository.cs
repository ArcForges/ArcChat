// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.LocalPersistence.Repositories;

/// <summary>
/// Stores messages for a conversation.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Appends messages in one serialized write operation.
    /// </summary>
    Task BulkAppendAsync(string conversationId, IReadOnlyList<Message> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one message by id.
    /// </summary>
    Task<Message?> GetAsync(string conversationId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists messages by append order.
    /// </summary>
    Task<IReadOnlyList<Message>> ListAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes one message.
    /// </summary>
    Task DeleteAsync(string conversationId, string messageId, CancellationToken cancellationToken = default);
}
