// Copyright (c) ArcForges. Licensed under the MIT License.

using Dapper;

namespace ArcChat.LocalPersistence.Sqlite;

internal sealed class LocalJsonTableStore
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly SqliteWriteQueue writeQueue;

    public LocalJsonTableStore(SqliteConnectionFactory connectionFactory, SqliteWriteQueue writeQueue)
    {
        this.connectionFactory = connectionFactory;
        this.writeQueue = writeQueue;
    }

    public async Task UpsertAsync(PersistenceTable table, string id, string json, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(json);
        string tableName = GetTableName(table);
        long updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await this.writeQueue.EnqueueAsync(
            async (connection, token) =>
            {
                string sql = $"""
                    INSERT INTO {tableName} (Id, Json, UpdatedAt)
                    VALUES (@id, @json, @updatedAt)
                    ON CONFLICT(Id) DO UPDATE SET
                      Json = excluded.Json,
                      UpdatedAt = excluded.UpdatedAt;
                    """;
                _ = await connection.ExecuteAsync(
                    new CommandDefinition(
                        sql,
                        new { id, json, updatedAt },
                        cancellationToken: token)).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetAsync(PersistenceTable table, string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        string tableName = GetTableName(table);
        await using Microsoft.Data.Sqlite.SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                $"SELECT Json FROM {tableName} WHERE Id = @id;",
                new { id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static string GetTableName(PersistenceTable table)
    {
        return table switch
        {
            PersistenceTable.Mask => "Mask",
            PersistenceTable.Plugin => "Plugin",
            PersistenceTable.McpServer => "McpServer",
            PersistenceTable.HtmlArtifactPreview => "HtmlArtifactPreview",
            PersistenceTable.Setting => "Setting",
            PersistenceTable.SyncMeta => "SyncMeta",
            PersistenceTable.SyncBackup => "SyncBackup",
            PersistenceTable.ToolCall => "ToolCall",
            PersistenceTable.PromptSeed => "PromptSeed",
            PersistenceTable.KeychainRef => "KeychainRef",
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, null),
        };
    }
}
