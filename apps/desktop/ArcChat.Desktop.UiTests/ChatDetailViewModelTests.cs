// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ArcChat.Agent;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class ChatDetailViewModelTests
{
    [Fact]
    public static async Task ChatDetailReplayStreamingDeltasUpdatesAssistantMessage()
    {
        ChatDetailViewModel viewModel = new ChatDetailViewModel("c1");

        await viewModel.ReplayEventsAsync(
            StreamEvents(
                new MessageDelta("c1", "a1", "hel"),
                new MessageDelta("c1", "a1", "lo"),
                new MessageCompleted("c1", "a1", Message.Text("a1", MessageRole.Assistant, "hello", "0")),
                new ChatFinished("c1", "a1", "stop")),
            "a1",
            CancellationToken.None).ConfigureAwait(true);

        MessageViewModel assistant = viewModel.Messages.Single(message => string.Equals(message.Id, "a1", StringComparison.Ordinal));
        _ = assistant.Text.Should().Be("hello");
        _ = assistant.IsStreaming.Should().BeFalse();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ChatDetailAbortCancelsStreamingRequest()
    {
        TaskCompletionSource<bool> started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ScriptedAgentRuntime runtime = new ScriptedAgentRuntime((request, cancellationToken) => AbortableStream(request, started, cancellationToken));
        await using ArcChatDatabase database = await CreateDatabaseAsync("c1").ConfigureAwait(true);
        ChatDetailViewModel viewModel = new ChatDetailViewModel("c1", runtime, database.Conversations, database.Messages);
        await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);

        viewModel.ComposerText = "hello";
        Task submitTask = viewModel.SubmitAsync(CancellationToken.None);
        _ = await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
        viewModel.Abort();
        await submitTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);

        _ = viewModel.StatusMessage.Should().Be("Aborted");
        _ = viewModel.IsStreaming.Should().BeFalse();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ChatDetailEditCreatesBranchRequest()
    {
        ScriptedAgentRuntime runtime = new ScriptedAgentRuntime(CompletedStream);
        await using ArcChatDatabase database = await CreateDatabaseAsync("c1").ConfigureAwait(true);
        ChatDetailViewModel viewModel = new ChatDetailViewModel("c1", runtime, database.Conversations, database.Messages);
        await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);
        MessageViewModel original = new MessageViewModel("u1", MessageRole.User, "old text", "0");
        viewModel.Messages.Add(original);

        viewModel.BeginEditCommand.Execute(original);
        original.DraftText = "new text";
        await viewModel.CommitEditCommand.ExecuteAsync(original).ConfigureAwait(true);

        MessageViewModel branchedUser = viewModel.Messages.Where(message => message.Role == MessageRole.User).Last();
        _ = branchedUser.Text.Should().Be("new text");
        _ = branchedUser.BranchOfMessageId.Should().Be("u1");
        _ = viewModel.LastBranchOfMessageId.Should().Be("u1");
        _ = runtime.Requests.Should().ContainSingle();
        _ = runtime.Requests[0].BranchOfMessageId.Should().Be("u1");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ChatDetailRegenerateCreatesAssistantSibling()
    {
        ScriptedAgentRuntime runtime = new ScriptedAgentRuntime(CompletedStream);
        await using ArcChatDatabase database = await CreateDatabaseAsync("c1").ConfigureAwait(true);
        Message[] messages =
        {
            Message.Text("u1", MessageRole.User, "hello", "0"),
            Message.Text("a1", MessageRole.Assistant, "first answer", "0"),
        };
        await database.Messages.BulkAppendAsync("c1", messages, CancellationToken.None).ConfigureAwait(true);
        ChatDetailViewModel viewModel = new ChatDetailViewModel("c1", runtime, database.Conversations, database.Messages);
        await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);

        await viewModel.RegenerateCommand.ExecuteAsync(null).ConfigureAwait(true);

        _ = runtime.Requests.Should().ContainSingle();
        _ = runtime.Requests[0].BranchOfMessageId.Should().Be("a1");
        _ = runtime.Requests[0].Messages.Select(message => message.Id).Should().Equal("u1");
        MessageViewModel originalAssistant = viewModel.Messages.Single(message => string.Equals(message.Id, "a1", StringComparison.Ordinal));
        _ = originalAssistant.HasAlternateBranches.Should().BeTrue();
        MessageViewModel regenerated = viewModel.Messages.Last();
        _ = regenerated.Role.Should().Be(MessageRole.Assistant);
        _ = regenerated.BranchOfMessageId.Should().Be("a1");
        IReadOnlyList<Message> stored = await database.Messages.ListAsync("c1", CancellationToken.None).ConfigureAwait(true);
        _ = stored[^1].BranchOfMessageId.Should().Be("a1");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    public static async Task ChatDetailColonCommandRunsBeforeNormalSend()
    {
        ScriptedAgentRuntime runtime = new ScriptedAgentRuntime(CompletedStream);
        await using ArcChatDatabase database = await CreateDatabaseAsync("c1").ConfigureAwait(true);
        ChatDetailViewModel viewModel = new ChatDetailViewModel("c1", runtime, database.Conversations, database.Messages);
        await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);
        viewModel.Messages.Add(new MessageViewModel("u1", MessageRole.User, "hello", "0"));

        viewModel.ComposerText = ":clear";
        await viewModel.SubmitAsync(CancellationToken.None).ConfigureAwait(true);

        _ = runtime.Requests.Should().BeEmpty();
        _ = viewModel.Messages.Should().ContainSingle(message => message.Role == MessageRole.User);
        _ = viewModel.StatusMessage.Should().Be("clear");
        Conversation? conversation = await database.Conversations.GetAsync("c1", CancellationToken.None).ConfigureAwait(true);
        _ = conversation!.ClearContextIndex.Should().Be(1);
    }

    [Fact]
    public static void MainWindowChatDestinationUsesChatDetailViewModel()
    {
        AppNavigator navigator = new AppNavigator();
        using SettingsViewModel settingsViewModel = new SettingsViewModel();
        using MainWindowViewModel viewModel = new MainWindowViewModel(
            navigator,
            new ConversationListViewModel(),
            settingsViewModel,
            new CommandPaletteViewModel(),
            null,
            conversationId => new ChatDetailViewModel(conversationId));

        navigator.Navigate(new Chat("c42"));

        ChatDetailViewModel chatDetail = viewModel.CurrentContent.Should().BeOfType<ChatDetailViewModel>().Subject;
        _ = chatDetail.ConversationId.Should().Be("c42");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Headless dispatch returns an implementation value ignored by the test.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait", Justification = "Headless Avalonia dispatch must remain on the UI thread.")]
    public static async Task ChatDetailComposerKeyboardSendsAndAborts()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            async () =>
            {
                TaskCompletionSource<bool> started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                ScriptedAgentRuntime runtime = new ScriptedAgentRuntime((request, cancellationToken) => AbortableStream(request, started, cancellationToken));
                await using ArcChatDatabase database = await CreateDatabaseAsync("c1").ConfigureAwait(true);
                ChatDetailViewModel viewModel = new ChatDetailViewModel("c1", runtime, database.Conversations, database.Messages);
                await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);
                ChatDetailView view = new ChatDetailView()
                {
                    DataContext = viewModel,
                };
                Window window = new Window()
                {
                    Width = 800,
                    Height = 600,
                    Content = view,
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();
                    TextBox composer = window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(textBox => string.Equals(textBox.Name, "Composer", StringComparison.Ordinal));
                    _ = composer.Focus(NavigationMethod.Tab, KeyModifiers.None);

                    viewModel.ComposerText = "hello";
                    window.KeyPress(Key.Enter, RawInputModifiers.Control, PhysicalKey.Enter, "\r");
                    _ = await started.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    await WaitUntilAsync(() => string.Equals(viewModel.StatusMessage, "Aborted", StringComparison.Ordinal)).ConfigureAwait(true);
                }
                finally
                {
                    window.Close();
                }

                _ = runtime.Requests.Should().ContainSingle();
            },
            CancellationToken.None).ConfigureAwait(true);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out waiting for condition.");
            }

            await Task.Delay(10).ConfigureAwait(true);
        }
    }

    private static async Task<ArcChatDatabase> CreateDatabaseAsync(string conversationId)
    {
        ArcChatDatabase database = new ArcChatDatabase(CreateDatabasePath());
        await database.InitializeAsync(CancellationToken.None).ConfigureAwait(true);
        await database.Conversations.UpsertAsync(CreateConversation(conversationId), CancellationToken.None).ConfigureAwait(true);
        return database;
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

    private static async IAsyncEnumerable<ChatEvent> StreamEvents(params ChatEvent[] events)
    {
        foreach (ChatEvent chatEvent in events)
        {
            await Task.Delay(1).ConfigureAwait(true);
            yield return chatEvent;
        }
    }

    private static async IAsyncEnumerable<ChatEvent> CompletedStream(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(true);
        yield return new MessageDelta(request.ConversationId, request.MessageId, "ok");
        yield return new MessageCompleted(
            request.ConversationId,
            request.MessageId,
            Message.Text(request.MessageId, MessageRole.Assistant, "ok", "0"));
        yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
    }

    private static async IAsyncEnumerable<ChatEvent> AbortableStream(
        AgentRequest request,
        TaskCompletionSource<bool> started,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        started.SetResult(true);
        yield return new MessageDelta(request.ConversationId, request.MessageId, "wait");
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(true);
        yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
    }

    private sealed class ScriptedAgentRuntime : IAgentRuntime
    {
        private readonly Func<AgentRequest, CancellationToken, IAsyncEnumerable<ChatEvent>> streamFactory;

        public ScriptedAgentRuntime(Func<AgentRequest, CancellationToken, IAsyncEnumerable<ChatEvent>> streamFactory)
        {
            this.streamFactory = streamFactory;
        }

        public List<AgentRequest> Requests { get; } = new List<AgentRequest>();

        public IAsyncEnumerable<ChatEvent> StreamAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            this.Requests.Add(request);
            return this.streamFactory(request, cancellationToken);
        }
    }
}
