// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.LocalPersistence.Sqlite;
using ArcChat.Protocol.Chat;
using Dapper;

namespace ArcChat.LocalPersistence.Repositories;

internal sealed class ConversationRepository : IConversationRepository
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly SqliteWriteQueue writeQueue;

    public ConversationRepository(SqliteConnectionFactory connectionFactory, SqliteWriteQueue writeQueue)
    {
        this.connectionFactory = connectionFactory;
        this.writeQueue = writeQueue;
    }

    public async Task UpsertAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        string json = StorageJson.Serialize(conversation, StorageJson.Context.Conversation);
        long updatedAt = conversation.LastUpdate == 0
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : conversation.LastUpdate;

        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO Conversation (Id, Topic, Json, UpdatedAt)
                        VALUES (@id, @topic, @json, @updatedAt)
                        ON CONFLICT(Id) DO UPDATE SET
                          Topic = excluded.Topic,
                          Json = excluded.Json,
                          UpdatedAt = excluded.UpdatedAt;

                        INSERT OR IGNORE INTO ConversationListState (
                          ConversationId,
                          IsPinned,
                          IsArchived,
                          SortOrder,
                          UnreadCount,
                          UpdatedAt
                        )
                        VALUES (
                          @id,
                          0,
                          0,
                          (SELECT COALESCE(MAX(SortOrder) + 1, 0) FROM ConversationListState),
                          0,
                          @updatedAt
                        );
                        """,
                        new { id = conversation.Id, topic = conversation.Topic, json, updatedAt },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Conversation?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        ConversationRow? row = await connection.QuerySingleOrDefaultAsync<ConversationRow>(
            new CommandDefinition(
                "SELECT Id, Topic, Json, UpdatedAt FROM Conversation WHERE Id = @id;",
                new { id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row is null ? null : StorageJson.Deserialize(row.Json, StorageJson.Context.Conversation);
    }

    public async Task<IReadOnlyList<Conversation>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<ConversationRow> rows = await connection.QueryAsync<ConversationRow>(
            new CommandDefinition(
                "SELECT Id, Topic, Json, UpdatedAt FROM Conversation ORDER BY UpdatedAt DESC;",
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(row => StorageJson.Deserialize(row.Json, StorageJson.Context.Conversation)).ToArray();
    }

    public async Task<IReadOnlyList<ConversationListEntry>> ListEntriesAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        int includeArchivedValue = includeArchived ? 1 : 0;
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<ConversationListRow> rows = await connection.QueryAsync<ConversationListRow>(
            new CommandDefinition(
                """
                SELECT
                  Conversation.Id,
                  Conversation.Topic,
                  Conversation.UpdatedAt,
                  COALESCE(ConversationListState.IsPinned, 0) AS IsPinned,
                  COALESCE(ConversationListState.IsArchived, 0) AS IsArchived,
                  COALESCE(ConversationListState.SortOrder, 2147483647) AS SortOrder,
                  COALESCE(ConversationListState.UnreadCount, 0) AS UnreadCount
                FROM Conversation
                LEFT JOIN ConversationListState
                  ON ConversationListState.ConversationId = Conversation.Id
                WHERE @includeArchived = 1
                   OR COALESCE(ConversationListState.IsArchived, 0) = 0
                ORDER BY
                  COALESCE(ConversationListState.IsPinned, 0) DESC,
                  COALESCE(ConversationListState.SortOrder, 2147483647) ASC,
                  Conversation.UpdatedAt DESC;
                """,
                new { includeArchived = includeArchivedValue },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(
            row => new ConversationListEntry(
                row.Id,
                row.Topic,
                row.UpdatedAt,
                row.IsPinned,
                row.IsArchived,
                row.SortOrder,
                row.UnreadCount)).ToArray();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    public async Task RenameAsync(string id, string topic, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        Conversation? current = await this.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return;
        }

        long updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Conversation renamed = current with { Topic = topic, LastUpdate = updatedAt };
        string json = StorageJson.Serialize(renamed, StorageJson.Context.Conversation);
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE Conversation
                        SET Topic = @topic,
                            Json = @json,
                            UpdatedAt = @updatedAt
                        WHERE Id = @id;

                        UPDATE ConversationListState
                        SET UpdatedAt = @updatedAt
                        WHERE ConversationId = @id;
                        """,
                        new { id, topic, json, updatedAt },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public Task SetPinnedAsync(string id, bool isPinned, CancellationToken cancellationToken = default)
    {
        return this.SetListStateAsync(id, "IsPinned", isPinned ? 1 : 0, cancellationToken);
    }

    public Task SetArchivedAsync(string id, bool isArchived, CancellationToken cancellationToken = default)
    {
        return this.SetListStateAsync(id, "IsArchived", isArchived ? 1 : 0, cancellationToken);
    }

    public async Task SetUnreadCountAsync(string id, int unreadCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfNegative(unreadCount);
        await this.EnsureListStateAndUpdateAsync(id, "UnreadCount", unreadCount, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReorderAsync(IReadOnlyList<string> orderedConversationIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedConversationIds);
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                await using System.Data.Common.DbTransaction transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
                for (int index = 0; index < orderedConversationIds.Count; index++)
                {
                    string id = orderedConversationIds[index];
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        throw new ArgumentException("Conversation ids must not be null or empty.", nameof(orderedConversationIds));
                    }

                    _ = await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            INSERT OR IGNORE INTO ConversationListState (
                              ConversationId,
                              IsPinned,
                              IsArchived,
                              SortOrder,
                              UnreadCount,
                              UpdatedAt
                            )
                            SELECT
                              Id,
                              0,
                              0,
                              @sortOrder,
                              0,
                              UpdatedAt
                            FROM Conversation
                            WHERE Id = @id;

                            UPDATE ConversationListState
                            SET SortOrder = @sortOrder,
                                UpdatedAt = @updatedAt
                            WHERE ConversationId = @id;
                            """,
                            new { id, sortOrder = index, updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                            transaction: transaction,
                            cancellationToken: token)).ConfigureAwait(false);
                }

                await transaction.CommitAsync(token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM Conversation WHERE Id = @id;",
                        new { id },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private Task SetListStateAsync(string id, string column, int value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return this.EnsureListStateAndUpdateAsync(id, column, value, cancellationToken);
    }

    private async Task EnsureListStateAndUpdateAsync(string id, string column, int value, CancellationToken cancellationToken)
    {
        long updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT OR IGNORE INTO ConversationListState (
                          ConversationId,
                          IsPinned,
                          IsArchived,
                          SortOrder,
                          UnreadCount,
                          UpdatedAt
                        )
                        SELECT
                          Id,
                          0,
                          0,
                          (SELECT COALESCE(MAX(SortOrder) + 1, 0) FROM ConversationListState),
                          0,
                          UpdatedAt
                        FROM Conversation
                        WHERE Id = @id;

                        UPDATE ConversationListState
                        SET IsPinned = CASE WHEN @column = 'IsPinned' THEN @value ELSE IsPinned END,
                            IsArchived = CASE WHEN @column = 'IsArchived' THEN @value ELSE IsArchived END,
                            UnreadCount = CASE WHEN @column = 'UnreadCount' THEN @value ELSE UnreadCount END,
                            UpdatedAt = @updatedAt
                        WHERE ConversationId = @id;
                        """,
                        new { id, column, value, updatedAt },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
