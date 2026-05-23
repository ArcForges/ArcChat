// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Data.Common;
using ArcChat.LocalPersistence.Sqlite;
using ArcChat.Protocol.Chat;
using Dapper;

namespace ArcChat.LocalPersistence.Repositories;

internal sealed class MessageRepository : IMessageRepository
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly SqliteWriteQueue writeQueue;

    public MessageRepository(SqliteConnectionFactory connectionFactory, SqliteWriteQueue writeQueue)
    {
        this.connectionFactory = connectionFactory;
        this.writeQueue = writeQueue;
    }

    public async Task BulkAppendAsync(string conversationId, IReadOnlyList<Message> messages, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
        {
            return;
        }

        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                await using DbTransaction transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
                int nextOrdinal = await connection.QuerySingleAsync<int>(
                    new CommandDefinition(
                        "SELECT COALESCE(MAX(Ordinal), -1) + 1 FROM Message WHERE ConversationId = @conversationId;",
                        new { conversationId },
                        transaction,
                        cancellationToken: token)).ConfigureAwait(false);

                foreach (Message message in messages)
                {
                    string json = StorageJson.Serialize(message, StorageJson.Context.Message);
                    _ = await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            INSERT INTO Message (ConversationId, Id, Ordinal, Json, BranchOfMessageId, CreatedAt)
                            VALUES (@conversationId, @id, @ordinal, @json, @branchOfMessageId, @createdAt)
                            ON CONFLICT(ConversationId, Id) DO UPDATE SET
                              Ordinal = excluded.Ordinal,
                              Json = excluded.Json,
                              BranchOfMessageId = excluded.BranchOfMessageId,
                              CreatedAt = excluded.CreatedAt;
                            """,
                            new
                            {
                                conversationId,
                                id = message.Id,
                                ordinal = nextOrdinal,
                                json,
                                branchOfMessageId = message.BranchOfMessageId,
                                createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            },
                            transaction,
                            cancellationToken: token)).ConfigureAwait(false);
                    nextOrdinal++;
                }

                await transaction.CommitAsync(token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Message?> GetAsync(string conversationId, string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        MessageRow? row = await connection.QuerySingleOrDefaultAsync<MessageRow>(
            new CommandDefinition(
                """
                SELECT ConversationId, Id, Ordinal, Json, BranchOfMessageId, CreatedAt
                FROM Message
                WHERE ConversationId = @conversationId AND Id = @messageId;
                """,
                new { conversationId, messageId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return row is null ? null : StorageJson.Deserialize(row.Json, StorageJson.Context.Message);
    }

    public async Task<IReadOnlyList<Message>> ListAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<MessageRow> rows = await connection.QueryAsync<MessageRow>(
            new CommandDefinition(
                """
                SELECT ConversationId, Id, Ordinal, Json, BranchOfMessageId, CreatedAt
                FROM Message
                WHERE ConversationId = @conversationId
                ORDER BY Ordinal ASC;
                """,
                new { conversationId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(row => StorageJson.Deserialize(row.Json, StorageJson.Context.Message)).ToArray();
    }

    public async Task<IReadOnlyList<Message>> ListBranchTreeAsync(string conversationId, string rootMessageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootMessageId);
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<MessageRow> rows = await connection.QueryAsync<MessageRow>(
            new CommandDefinition(
                """
                WITH RECURSIVE BranchTree(Id, Ordinal, Json) AS (
                  SELECT Id, Ordinal, Json
                  FROM Message
                  WHERE ConversationId = @conversationId AND Id = @rootMessageId
                  UNION ALL
                  SELECT Child.Id, Child.Ordinal, Child.Json
                  FROM Message AS Child
                  INNER JOIN BranchTree AS Parent ON Child.BranchOfMessageId = Parent.Id
                  WHERE Child.ConversationId = @conversationId
                )
                SELECT Id, Ordinal, Json
                FROM BranchTree
                ORDER BY Ordinal ASC;
                """,
                new { conversationId, rootMessageId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.Select(row => StorageJson.Deserialize(row.Json, StorageJson.Context.Message)).ToArray();
    }

    public async Task DeleteAsync(string conversationId, string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM Message WHERE ConversationId = @conversationId AND Id = @messageId;",
                        new { conversationId, messageId },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
