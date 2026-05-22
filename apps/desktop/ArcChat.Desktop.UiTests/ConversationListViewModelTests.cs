// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.LocalPersistence;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class ConversationListViewModelTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ConversationListReorderPersists()
    {
        string path = CreateDatabasePath();
        await using ArcChatDatabase database = new ArcChatDatabase(path);
        await database.InitializeAsync(CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c1"), CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c2"), CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c3"), CancellationToken.None);
        ConversationListViewModel viewModel = new ConversationListViewModel(database.Conversations);
        await viewModel.LoadAsync(CancellationToken.None);

        await viewModel.MoveAsync("c3", 0, CancellationToken.None);

        await using ArcChatDatabase reopened = new ArcChatDatabase(path);
        await reopened.InitializeAsync(CancellationToken.None);
        ConversationListViewModel reloaded = new ConversationListViewModel(reopened.Conversations);
        await reloaded.LoadAsync(CancellationToken.None);
        _ = reloaded.Conversations.Select(item => item.Id).Should().Equal("c3", "c1", "c2");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ConversationListPinnedItemsSortFirst()
    {
        string path = CreateDatabasePath();
        await using ArcChatDatabase database = new ArcChatDatabase(path);
        await database.InitializeAsync(CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c1"), CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c2"), CancellationToken.None);
        await database.Conversations.UpsertAsync(CreateConversation("c3"), CancellationToken.None);
        ConversationListViewModel viewModel = new ConversationListViewModel(database.Conversations);
        await viewModel.LoadAsync(CancellationToken.None);
        ConversationListItem item = viewModel.Conversations.Single(conversation => string.Equals(conversation.Id, "c2", StringComparison.Ordinal));

        await viewModel.PinCommand.ExecuteAsync(item);

        _ = viewModel.Conversations[0].Id.Should().Be("c2");
        _ = viewModel.Conversations[0].IsPinned.Should().BeTrue();
    }

    private static string CreateDatabasePath()
    {
        string directory = Path.Join(Path.GetTempPath(), "ArcChat.Desktop.UiTests");
        Directory.CreateDirectory(directory);
        return Path.Join(directory, Guid.NewGuid().ToString("N") + ".db");
    }

    private static Conversation CreateConversation(string id)
    {
        return new Conversation(
            id,
            "Topic " + id,
            string.Empty,
            ImmutableArray<Message>.Empty,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            CreateMask("mask-" + id));
    }

    private static Mask CreateMask(string id)
    {
        return new Mask(
            id,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            "1f600",
            "Mask " + id,
            false,
            ImmutableArray<Message>.Empty,
            true,
            ModelConfig.NextChatDefault,
            "en",
            false,
            ImmutableArray<string>.Empty);
    }
}
