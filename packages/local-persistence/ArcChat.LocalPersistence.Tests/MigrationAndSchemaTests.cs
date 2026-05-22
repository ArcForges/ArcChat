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
            string id = table.ToString() + "-1";
            string json = "{\"table\":\"" + table.ToString() + "\"}";
            await database.JsonTables.UpsertAsync(table, id, json, CancellationToken.None);

            string? stored = await database.JsonTables.GetAsync(table, id, CancellationToken.None);

            _ = stored.Should().Be(json);
        }
    }
}
