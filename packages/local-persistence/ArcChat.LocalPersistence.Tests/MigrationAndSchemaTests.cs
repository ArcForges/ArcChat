// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.LocalPersistence.Sqlite;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ArcChat.LocalPersistence.Tests;

public sealed class MigrationAndSchemaTests
{
    [Fact]
    public async Task MigrationCreatesVersionOneTablesAndEnablesWal()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);

        await database.InitializeAsync(CancellationToken.None);

        await using SqliteConnection connection = new($"Data Source={path};Mode=ReadWrite;Cache=Shared");
        await connection.OpenAsync(CancellationToken.None);
        HashSet<string> tables = new(StringComparer.Ordinal);
        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
            await using SqliteDataReader reader = await command.ExecuteReaderAsync(CancellationToken.None);
            while (await reader.ReadAsync(CancellationToken.None))
            {
                tables.Add(reader.GetString(0));
            }
        }

        _ = tables.Should().Contain(new[]
        {
            "Conversation",
            "Message",
            "Mask",
            "Plugin",
            "McpServer",
            "HtmlArtifactPreview",
            "Setting",
            "SyncMeta",
            "SyncBackup",
            "ToolCall",
            "PromptSeed",
            "KeychainRef",
        });

        await using SqliteCommand journalCommand = connection.CreateCommand();
        journalCommand.CommandText = "PRAGMA journal_mode;";
        object? journalMode = await journalCommand.ExecuteScalarAsync(CancellationToken.None);
        _ = journalMode.Should().Be("wal");
    }

    [Fact]
    public async Task JsonStoreRoundTripsEveryAuxiliaryTable()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);

        foreach (PersistenceTable table in Enum.GetValues<PersistenceTable>())
        {
            string id = table + "-1";
            string json = "{\"table\":\"" + table + "\"}";
            await database.JsonTables.UpsertAsync(table, id, json, CancellationToken.None);

            string? stored = await database.JsonTables.GetAsync(table, id, CancellationToken.None);

            _ = stored.Should().Be(json);
        }
    }

    [Fact]
    public async Task MigrationAddsBranchColumnToVersionOneMessages()
    {
        string path = TestData.CreateDatabasePath();
        await CreateVersionOneConversationAndMessageTablesAsync(path);

        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);

        await using SqliteConnection connection = new($"Data Source={path};Mode=ReadWrite;Cache=Shared");
        await connection.OpenAsync(CancellationToken.None);
        HashSet<string> columns = new(StringComparer.Ordinal);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Message);";
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(CancellationToken.None);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            columns.Add(reader.GetString(1));
        }

        _ = columns.Should().Contain("BranchOfMessageId");
    }

    private static async Task CreateVersionOneConversationAndMessageTablesAsync(string path)
    {
        await using SqliteConnection connection = new($"Data Source={path};Mode=ReadWriteCreate;Cache=Shared");
        await connection.OpenAsync(CancellationToken.None);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE Conversation (
              Id TEXT NOT NULL PRIMARY KEY,
              Topic TEXT NOT NULL,
              Json TEXT NOT NULL,
              UpdatedAt INTEGER NOT NULL
            );

            CREATE TABLE Message (
              ConversationId TEXT NOT NULL,
              Id TEXT NOT NULL,
              Ordinal INTEGER NOT NULL,
              Json TEXT NOT NULL,
              CreatedAt INTEGER NOT NULL,
              PRIMARY KEY (ConversationId, Id),
              FOREIGN KEY (ConversationId) REFERENCES Conversation(Id) ON DELETE CASCADE
            );
            """;
        _ = await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
