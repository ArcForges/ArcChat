// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.LocalPersistence.Repositories;

internal sealed class ConversationRow
{
    public string Id { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;

    public long UpdatedAt { get; set; }
}

internal sealed class MessageRow
{
    public string ConversationId { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public string Json { get; set; } = string.Empty;

    public long CreatedAt { get; set; }
}
