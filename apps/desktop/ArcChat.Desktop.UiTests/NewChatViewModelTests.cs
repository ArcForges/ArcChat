// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Conversations;
using ArcChat.Desktop.Features.Masks;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence;
using ArcChat.LocalPersistence.Repositories;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Settings;
using FluentAssertions;
using Xunit;
using SettingsRepository = ArcChat.LocalServices.Settings.ISettingsRepository;

namespace ArcChat.Desktop.UiTests;

public sealed class NewChatViewModelTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task NewChatFlowNavigatesFromHomeThroughNewChatToChatDetail()
    {
        await using ArcChatDatabase database = await CreateDatabaseAsync().ConfigureAwait(true);
        AppNavigator navigator = new AppNavigator();
        using SettingsViewModel settingsViewModel = new SettingsViewModel();
        using MainWindowViewModel shell = new MainWindowViewModel(
            navigator,
            new ConversationListViewModel(database.Conversations, navigator),
            settingsViewModel,
            new CommandPaletteViewModel(),
            null,
            conversationId => new ChatDetailViewModel(conversationId),
            null,
            () => CreateLoadedNewChat(database, navigator));

        shell.NavigateCommand.Execute("new-chat");
        NewChatViewModel newChat = shell.CurrentContent.Should().BeOfType<NewChatViewModel>().Subject;

        await newChat.StartBlankCommand.ExecuteAsync(null).ConfigureAwait(true);

        ChatDetailViewModel chatDetail = shell.CurrentContent.Should().BeOfType<ChatDetailViewModel>().Subject;
        _ = navigator.Current.Should().BeOfType<Chat>();
        _ = chatDetail.ConversationId.Should().NotBeNullOrWhiteSpace();
        IReadOnlyList<ConversationListEntry> entries =
            await database.Conversations.ListEntriesAsync(false, CancellationToken.None).ConfigureAwait(true);
        _ = entries.Should().ContainSingle(entry => entry.Id == chatDetail.ConversationId && entry.IsPinned);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task NewChatMaskAppliesContextAndModelOverrides()
    {
        await using ArcChatDatabase database = await CreateDatabaseAsync().ConfigureAwait(true);
        AppNavigator navigator = new AppNavigator();
        NewChatViewModel newChat = CreateLoadedNewChat(database, navigator);
        RecommendedMaskItem mask = newChat.RecommendedMasks.Single(item => string.Equals(item.Name, "GitHub Copilot", StringComparison.Ordinal));

        await newChat.StartWithMaskCommand.ExecuteAsync(mask).ConfigureAwait(true);

        Chat destination = navigator.Current.Should().BeOfType<Chat>().Subject;
        Conversation? conversation = await database.Conversations.GetAsync(destination.ConversationId, CancellationToken.None).ConfigureAwait(true);
        _ = conversation.Should().NotBeNull();
        _ = conversation!.Topic.Should().Be("GitHub Copilot");
        _ = conversation.Mask.Context.Should().ContainSingle(message => message.Role == MessageRole.System);
        string prompt = conversation.Mask.Context[0].Content.OfType<TextBlock>().Single().Text;
        _ = prompt.Should().StartWith("You are an AI programming assistant.");
        _ = conversation.Mask.ModelConfig.Model.Should().Be("gpt-4");
        _ = conversation.Mask.ModelConfig.Temperature.Should().Be(0.3);
        _ = conversation.Mask.ModelConfig.MaxTokens.Should().Be(2000);
    }

    private static NewChatViewModel CreateLoadedNewChat(ArcChatDatabase database, IAppNavigator navigator)
    {
        NewChatViewModel viewModel = new NewChatViewModel(
            database.Conversations,
            new InMemorySettingsRepository(SettingsDefaults.Create()),
            navigator);
        viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        return viewModel;
    }

    private static async Task<ArcChatDatabase> CreateDatabaseAsync()
    {
        ArcChatDatabase database = new ArcChatDatabase(CreateDatabasePath());
        await database.InitializeAsync(CancellationToken.None).ConfigureAwait(true);
        return database;
    }

    private static string CreateDatabasePath()
    {
        string directory = Path.Join(Path.GetTempPath(), "ArcChat.Desktop.UiTests");
        Directory.CreateDirectory(directory);
        return Path.Join(directory, Guid.NewGuid().ToString("N") + ".db");
    }

    private sealed class InMemorySettingsRepository : SettingsRepository
    {
        private SettingsSnapshot snapshot;

        public InMemorySettingsRepository(SettingsSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.snapshot);
        }

        public Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            this.snapshot = snapshot;
            return Task.CompletedTask;
        }

        public IObservable<T> Observe<T>(KeyExpression<T> keyExpression)
        {
            return new SnapshotObservable<T>(this.snapshot, keyExpression);
        }
    }

    private sealed class SnapshotObservable<T> : IObservable<T>
    {
        private readonly SettingsSnapshot snapshot;
        private readonly KeyExpression<T> keyExpression;

        public SnapshotObservable(SettingsSnapshot snapshot, KeyExpression<T> keyExpression)
        {
            this.snapshot = snapshot;
            this.keyExpression = keyExpression;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(this.keyExpression.Evaluate(this.snapshot));
            return EmptyDisposable.Instance;
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new EmptyDisposable();

        public void Dispose()
        {
        }
    }
}
