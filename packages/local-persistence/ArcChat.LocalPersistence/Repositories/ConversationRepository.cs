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
}
