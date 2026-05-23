// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.LocalPersistence.Repositories;

/// <summary>
/// Conversation metadata used by the desktop conversation list.
/// </summary>
/// <param name="Id">Conversation id.</param>
/// <param name="Topic">Conversation title.</param>
/// <param name="UpdatedAt">Conversation update time in Unix epoch milliseconds.</param>
/// <param name="IsPinned">Whether the conversation is pinned above unpinned conversations.</param>
/// <param name="IsArchived">Whether the conversation is hidden from the active list.</param>
/// <param name="SortOrder">User-controlled list order.</param>
/// <param name="UnreadCount">Unread message count.</param>
public sealed record ConversationListEntry(
    string Id,
    string Topic,
    long UpdatedAt,
    bool IsPinned,
    bool IsArchived,
    int SortOrder,
    int UnreadCount);
