// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ArcChat.LocalPersistence.Tests;

public sealed class RepositoryContractTests
{
    [Fact]
    public async Task ConversationRepositoryHandlesEmptyConversation()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);
        Conversation conversation = TestData.CreateConversation("empty");

        await database.Conversations.UpsertAsync(conversation, CancellationToken.None);

        Conversation? stored = await database.Conversations.GetAsync("empty", CancellationToken.None);
        IReadOnlyList<Conversation> all = await database.Conversations.ListAsync(CancellationToken.None);

        _ = stored.Should().Be(conversation);
        _ = all.Should().ContainSingle().Which.Should().Be(conversation);
    }

    [Fact]
    public async Task MessageRepositoryHandlesSingleAndLargeBulkAppend()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);
        await database.Conversations.UpsertAsync(TestData.CreateConversation("c1"), CancellationToken.None);
        Message single = TestData.CreateMessage("m-single", "single");
        Message[] many = Enumerable.Range(0, 5_000)
            .Select(index => TestData.CreateMessage("m-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), "body " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();

        await database.Messages.BulkAppendAsync("c1", new[] { single }, CancellationToken.None);
        await database.Messages.BulkAppendAsync("c1", many, CancellationToken.None);

        Message? storedSingle = await database.Messages.GetAsync("c1", "m-single", CancellationToken.None);
        IReadOnlyList<Message> stored = await database.Messages.ListAsync("c1", CancellationToken.None);

        _ = storedSingle.Should().BeEquivalentTo(single);
        _ = stored.Should().HaveCount(5_001);
        _ = stored[0].Id.Should().Be("m-single");
        _ = stored[^1].Id.Should().Be("m-4999");
    }

    [Fact]
    public async Task SettingsRepositoryKeepsKeychainReferencesOpaque()
    {
        const string KeychainReference = "keychain://provider/openai/default";
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);

        await database.Settings.UpsertSnapshotAsync(TestData.CreateSettingsSnapshot(KeychainReference), CancellationToken.None);
        await database.Settings.UpsertValueAsync("sync.webdav.passwordRef", KeychainReference, CancellationToken.None);

        _ = (await database.Settings.GetSnapshotAsync(CancellationToken.None))!.Realtime.ApiKeyRef.Should().Be(KeychainReference);
        string? storedReference = await database.Settings.GetValueAsync("sync.webdav.passwordRef", CancellationToken.None);
        _ = storedReference.Should().Be(KeychainReference);
    }

    [Fact]
    public async Task ConcurrentAppendsAreSerialized()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);
        await database.Conversations.UpsertAsync(TestData.CreateConversation("stream"), CancellationToken.None);

        Task[] appends = Enumerable.Range(0, 20)
            .Select(batch => database.Messages.BulkAppendAsync(
                "stream",
                Enumerable.Range(0, 10)
                    .Select(index => TestData.CreateMessage("m-" + batch.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), "chunk"))
                    .ToArray(),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(appends);

        IReadOnlyList<Message> stored = await database.Messages.ListAsync("stream", CancellationToken.None);
        _ = stored.Should().HaveCount(200);
        _ = stored.Select(message => message.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ConcurrentReaderWriterStressKeepsDatabaseValid()
    {
        string path = TestData.CreateDatabasePath();
        await using ArcChatDatabase database = new(path);
        await database.InitializeAsync(CancellationToken.None);
        await database.Conversations.UpsertAsync(TestData.CreateConversation("stress"), CancellationToken.None);

        Task writer = Task.Run(async () =>
        {
            for (int index = 0; index < 100; index++)
            {
                await database.Messages.BulkAppendAsync(
                    "stress",
                    new[] { TestData.CreateMessage("stress-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), "body") },
                    CancellationToken.None);
            }
        });

        Task reader = Task.Run(async () =>
        {
            for (int index = 0; index < 100; index++)
            {
                _ = await database.Messages.ListAsync("stress", CancellationToken.None);
            }
        });

        await Task.WhenAll(writer, reader);

        IReadOnlyList<Message> stored = await database.Messages.ListAsync("stress", CancellationToken.None);
        _ = stored.Should().HaveCount(100);

        await using SqliteConnection connection = new($"Data Source={path};Mode=ReadWrite;Cache=Shared");
        await connection.OpenAsync(CancellationToken.None);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        object? integrity = await command.ExecuteScalarAsync(CancellationToken.None);
        _ = integrity.Should().Be("ok");
    }
}
