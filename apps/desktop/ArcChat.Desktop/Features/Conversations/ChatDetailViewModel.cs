// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using ArcChat.Agent;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence.Repositories;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ChatDetailViewModel : ViewModelBase
{
    private readonly IAgentRuntime? agentRuntime;
    private readonly IConversationRepository? conversationRepository;
    private readonly IMessageRepository? messageRepository;
    private readonly IAppNavigator? navigator;
    private readonly IConversationTitler? conversationTitler;
    private readonly IContextSummarizer? contextSummarizer;
    private readonly ConversationExportService? exportService;
    private readonly IShareService? shareService;
    private Conversation? conversation;
    private CancellationTokenSource? streamCancellation;
    private string composerText = string.Empty;
    private bool isStreaming;
    private bool isExporterVisible;
    private string statusMessage = string.Empty;
    private string lastCopiedText = string.Empty;
    private string lastSharedText = string.Empty;
    private string? lastBranchOfMessageId;
    private ExporterViewModel? exporter;

    public ChatDetailViewModel()
        : this("design-chat")
    {
        this.Messages.Add(new MessageViewModel("design-user", MessageRole.User, "Hello", "2026-05-22"));
        this.Messages.Add(new MessageViewModel("design-assistant", MessageRole.Assistant, "Echo: Hello", "2026-05-22"));
    }

    internal ChatDetailViewModel(string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        this.ConversationId = conversationId;
        this.SendCommand = new AsyncRelayCommand(this.SubmitAsync);
        this.AbortCommand = new RelayCommand(this.Abort);
        this.RegenerateCommand = new AsyncRelayCommand(this.RegenerateAsync);
        this.CopyCommand = new RelayCommand<MessageViewModel>(this.Copy);
        this.ShareCommand = new RelayCommand<MessageViewModel>(this.Share);
        this.DeleteMessageCommand = new AsyncRelayCommand<MessageViewModel>(this.DeleteMessageAsync);
        this.BeginEditCommand = new RelayCommand<MessageViewModel>(this.BeginEdit);
        this.CommitEditCommand = new AsyncRelayCommand<MessageViewModel>(this.CommitEditAsync);
        this.CancelEditCommand = new RelayCommand<MessageViewModel>(this.CancelEdit);
        this.ToggleExporterCommand = new RelayCommand(this.ToggleExporter);
    }

    internal ChatDetailViewModel(
        string conversationId,
        IAgentRuntime agentRuntime,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IAppNavigator? navigator = null,
        IConversationTitler? conversationTitler = null,
        IContextSummarizer? contextSummarizer = null,
        ConversationExportService? exportService = null,
        IShareService? shareService = null)
        : this(conversationId)
    {
        this.agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
        this.conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        this.messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        this.navigator = navigator;
        this.conversationTitler = conversationTitler;
        this.contextSummarizer = contextSummarizer;
        this.exportService = exportService;
        this.shareService = shareService;
    }

    public string ConversationId { get; }

    public ObservableCollection<MessageViewModel> Messages { get; } = new ObservableCollection<MessageViewModel>();

    public IAsyncRelayCommand SendCommand { get; }

    public IRelayCommand AbortCommand { get; }

    public IAsyncRelayCommand RegenerateCommand { get; }

    public IRelayCommand<MessageViewModel> CopyCommand { get; }

    public IRelayCommand<MessageViewModel> ShareCommand { get; }

    public IAsyncRelayCommand<MessageViewModel> DeleteMessageCommand { get; }

    public IRelayCommand<MessageViewModel> BeginEditCommand { get; }

    public IAsyncRelayCommand<MessageViewModel> CommitEditCommand { get; }

    public IRelayCommand<MessageViewModel> CancelEditCommand { get; }

    public IRelayCommand ToggleExporterCommand { get; }

    public ExporterViewModel? Exporter
    {
        get => this.exporter;
        private set => this.SetProperty(ref this.exporter, value);
    }

    public bool IsExporterVisible
    {
        get => this.isExporterVisible;
        private set => this.SetProperty(ref this.isExporterVisible, value);
    }

    public string ComposerText
    {
        get => this.composerText;
        set => this.SetProperty(ref this.composerText, value);
    }

    public bool IsStreaming
    {
        get => this.isStreaming;
        private set => this.SetProperty(ref this.isStreaming, value);
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    public string LastCopiedText
    {
        get => this.lastCopiedText;
        private set => this.SetProperty(ref this.lastCopiedText, value);
    }

    public string LastSharedText
    {
        get => this.lastSharedText;
        private set => this.SetProperty(ref this.lastSharedText, value);
    }

    public string? LastBranchOfMessageId
    {
        get => this.lastBranchOfMessageId;
        private set => this.SetProperty(ref this.lastBranchOfMessageId, value);
    }

    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (this.conversationRepository is null || this.messageRepository is null)
        {
            return;
        }

        this.conversation = await this.conversationRepository.GetAsync(this.ConversationId, cancellationToken).ConfigureAwait(true);
        IReadOnlyList<Message> messages = await this.messageRepository.ListAsync(this.ConversationId, cancellationToken).ConfigureAwait(true);
        this.Messages.Clear();
        foreach (Message message in messages)
        {
            this.Messages.Add(MessageViewModel.FromMessage(message));
        }

        this.RefreshExporter();
    }

    internal async Task SubmitAsync(CancellationToken cancellationToken = default)
    {
        string input = this.ComposerText.Trim();
        if (input.Length == 0)
        {
            return;
        }

        if (await this.TryRunCommandAsync(input, cancellationToken).ConfigureAwait(true))
        {
            this.ComposerText = string.Empty;
            return;
        }

        this.ComposerText = string.Empty;
        await this.SubmitUserMessageAsync(input, null, cancellationToken).ConfigureAwait(true);
    }

    internal void Abort()
    {
        this.streamCancellation?.Cancel();
    }

    internal async Task ReplayEventsAsync(IAsyncEnumerable<ChatEvent> events, string assistantMessageId, CancellationToken cancellationToken = default)
    {
        MessageViewModel assistant = this.EnsureAssistantMessage(assistantMessageId, null);
        await foreach (ChatEvent chatEvent in events.WithCancellation(cancellationToken).ConfigureAwait(true))
        {
            await this.ApplyStreamEventAsync(assistant, chatEvent, cancellationToken).ConfigureAwait(true);
        }
    }

    private static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static ModelDescriptor CreateModelDescriptor(ModelConfig modelConfig)
    {
        return new ModelDescriptor(
            modelConfig.Model,
            modelConfig.Model,
            modelConfig.ProviderName,
            true,
            0,
            ImmutableArray<ProviderCapability>.Empty,
            ContextWindow: modelConfig.MaxTokens);
    }

    private async Task SubmitUserMessageAsync(string text, string? branchOfMessageId, CancellationToken cancellationToken)
    {
        MessageViewModel userMessage = new MessageViewModel(
            NewId(),
            MessageRole.User,
            text,
            DateTimeOffset.Now.ToString("g", System.Globalization.CultureInfo.CurrentCulture),
            branchOfMessageId: branchOfMessageId);
        this.Messages.Add(userMessage);
        this.LastBranchOfMessageId = branchOfMessageId;
        if (this.messageRepository is not null)
        {
            await this.messageRepository.BulkAppendAsync(this.ConversationId, new[] { userMessage.ToMessage() }, cancellationToken).ConfigureAwait(true);
            await this.SaveConversationSnapshotAsync(cancellationToken).ConfigureAwait(true);
        }

        await this.RunAssistantAsync(branchOfMessageId, cancellationToken).ConfigureAwait(true);
    }

    private async Task RunAssistantAsync(string? branchOfMessageId, CancellationToken cancellationToken)
    {
        if (this.agentRuntime is null || this.IsStreaming)
        {
            return;
        }

        string assistantMessageId = NewId();
        MessageViewModel assistant = this.EnsureAssistantMessage(assistantMessageId, branchOfMessageId);
        this.streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.IsStreaming = true;
        this.StatusMessage = string.Empty;
        try
        {
            AgentRequest request = new AgentRequest(
                this.ConversationId,
                assistantMessageId,
                this.Messages
                    .Where(message => !string.Equals(message.Id, assistantMessageId, StringComparison.Ordinal))
                    .Select(message => message.ToMessage())
                    .ToArray(),
                this.GetModelConfig(),
                branchOfMessageId,
                maxTransientRetries: 1,
                transientRetryDelay: default);

            await foreach (ChatEvent chatEvent in this.agentRuntime.StreamAsync(request, this.streamCancellation.Token).ConfigureAwait(true))
            {
                await this.ApplyStreamEventAsync(assistant, chatEvent, this.streamCancellation.Token).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            assistant.IsStreaming = false;
            this.StatusMessage = "Aborted";
        }
        finally
        {
            this.IsStreaming = false;
            this.streamCancellation.Dispose();
            this.streamCancellation = null;
        }
    }

    private async Task ApplyStreamEventAsync(MessageViewModel assistant, ChatEvent chatEvent, CancellationToken cancellationToken)
    {
        switch (chatEvent)
        {
            case MessageDelta delta:
                assistant.AppendDelta(delta.Delta);
                break;
            case MessageCompleted completed:
                assistant.Text = MessageViewModel.ExtractText(completed.Message);
                assistant.IsStreaming = false;
                assistant.IsError = completed.Message.IsError;
                if (this.messageRepository is not null)
                {
                    await this.messageRepository.BulkAppendAsync(this.ConversationId, new[] { assistant.ToMessage() }, cancellationToken).ConfigureAwait(true);
                    await this.SaveConversationSnapshotAsync(cancellationToken).ConfigureAwait(true);
                    await this.RefreshConversationMetadataAsync(cancellationToken).ConfigureAwait(true);
                }

                break;
            case ChatError error:
                assistant.Text = error.Message;
                assistant.IsError = true;
                assistant.IsStreaming = false;
                this.StatusMessage = error.Code;
                break;
            case ChatFinished:
                assistant.IsStreaming = false;
                break;
        }
    }

    private async Task RegenerateAsync()
    {
        MessageViewModel? lastUser = this.Messages.LastOrDefault(message => message.Role == MessageRole.User);
        if (lastUser is null)
        {
            return;
        }

        await this.SubmitUserMessageAsync(lastUser.Text, lastUser.Id, CancellationToken.None).ConfigureAwait(true);
    }

    private void Copy(MessageViewModel? message)
    {
        this.LastCopiedText = message?.Text ?? string.Empty;
    }

    private void Share(MessageViewModel? message)
    {
        this.LastSharedText = message?.Text ?? string.Empty;
    }

    private void ToggleExporter()
    {
        this.RefreshExporter();
        this.IsExporterVisible = !this.IsExporterVisible;
    }

    private async Task DeleteMessageAsync(MessageViewModel? message)
    {
        if (message is null)
        {
            return;
        }

        _ = this.Messages.Remove(message);
        if (this.messageRepository is not null)
        {
            await this.messageRepository.DeleteAsync(this.ConversationId, message.Id, CancellationToken.None).ConfigureAwait(true);
            await this.SaveConversationSnapshotAsync(CancellationToken.None).ConfigureAwait(true);
        }
    }

    private void BeginEdit(MessageViewModel? message)
    {
        if (message is null)
        {
            return;
        }

        message.DraftText = message.Text;
        message.IsEditing = true;
    }

    private async Task CommitEditAsync(MessageViewModel? message)
    {
        if (message is null)
        {
            return;
        }

        string editedText = message.DraftText.Trim();
        message.IsEditing = false;
        if (editedText.Length == 0)
        {
            return;
        }

        await this.SubmitUserMessageAsync(editedText, message.Id, CancellationToken.None).ConfigureAwait(true);
    }

    private void CancelEdit(MessageViewModel? message)
    {
        if (message is null)
        {
            return;
        }

        message.DraftText = message.Text;
        message.IsEditing = false;
    }

    private async Task<bool> TryRunCommandAsync(string input, CancellationToken cancellationToken)
    {
        if (input.Length < 2 || (input[0] != ':' && input[0] != '：'))
        {
            return false;
        }

        string command = input[1..].Trim();
        switch (command)
        {
            case "new":
            case "newm":
                this.navigator?.Navigate(new NewChat());
                this.StatusMessage = command;
                return true;
            case "prev":
                await this.NavigateSiblingAsync(-1, cancellationToken).ConfigureAwait(true);
                return true;
            case "next":
                await this.NavigateSiblingAsync(1, cancellationToken).ConfigureAwait(true);
                return true;
            case "clear":
                await this.ClearContextAsync(cancellationToken).ConfigureAwait(true);
                return true;
            case "fork":
                await this.ForkConversationAsync(cancellationToken).ConfigureAwait(true);
                return true;
            case "del":
                await this.DeleteConversationAsync(cancellationToken).ConfigureAwait(true);
                return true;
            default:
                return false;
        }
    }

    private async Task NavigateSiblingAsync(int delta, CancellationToken cancellationToken)
    {
        if (this.conversationRepository is null || this.navigator is null)
        {
            this.StatusMessage = delta < 0 ? "prev" : "next";
            return;
        }

        IReadOnlyList<ConversationListEntry> entries = await this.conversationRepository.ListEntriesAsync(false, cancellationToken).ConfigureAwait(true);
        int currentIndex = entries.ToList().FindIndex(entry => string.Equals(entry.Id, this.ConversationId, StringComparison.Ordinal));
        if (currentIndex < 0 || entries.Count == 0)
        {
            return;
        }

        int nextIndex = (currentIndex + delta + entries.Count) % entries.Count;
        this.navigator.Navigate(new Chat(entries[nextIndex].Id));
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private async Task ClearContextAsync(CancellationToken cancellationToken)
    {
        if (this.conversationRepository is not null)
        {
            Conversation conversationToSave = this.EnsureConversation() with
            {
                MemoryPrompt = string.Empty,
                ClearContextIndex = this.Messages.Count,
            };
            this.conversation = conversationToSave;
            await this.conversationRepository.UpsertAsync(conversationToSave, cancellationToken).ConfigureAwait(true);
        }

        this.StatusMessage = "clear";
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private async Task ForkConversationAsync(CancellationToken cancellationToken)
    {
        if (this.conversationRepository is null || this.messageRepository is null)
        {
            this.StatusMessage = "fork";
            return;
        }

        string forkId = NewId();
        Conversation source = this.EnsureConversation();
        Conversation fork = source with
        {
            Id = forkId,
            Topic = source.Topic + " Copy",
            Messages = this.Messages.Select(message => message.ToMessage()).ToImmutableArray(),
            LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        await this.conversationRepository.UpsertAsync(fork, cancellationToken).ConfigureAwait(true);
        await this.messageRepository.BulkAppendAsync(forkId, fork.Messages, cancellationToken).ConfigureAwait(true);
        this.navigator?.Navigate(new Chat(forkId));
        this.StatusMessage = "fork";
    }

    private async Task DeleteConversationAsync(CancellationToken cancellationToken)
    {
        if (this.conversationRepository is not null)
        {
            await this.conversationRepository.DeleteAsync(this.ConversationId, cancellationToken).ConfigureAwait(true);
        }

        this.navigator?.Navigate(new Home());
        this.StatusMessage = "del";
    }

    private MessageViewModel EnsureAssistantMessage(string assistantMessageId, string? branchOfMessageId)
    {
        MessageViewModel assistant = new MessageViewModel(
            assistantMessageId,
            MessageRole.Assistant,
            string.Empty,
            DateTimeOffset.Now.ToString("g", System.Globalization.CultureInfo.CurrentCulture),
            isStreaming: true,
            branchOfMessageId: branchOfMessageId);
        this.Messages.Add(assistant);
        return assistant;
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private async Task SaveConversationSnapshotAsync(CancellationToken cancellationToken)
    {
        if (this.conversationRepository is null)
        {
            return;
        }

        Conversation snapshot = this.EnsureConversation() with
        {
            Messages = this.Messages.Select(message => message.ToMessage()).ToImmutableArray(),
            LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        this.conversation = snapshot;
        await this.conversationRepository.UpsertAsync(snapshot, cancellationToken).ConfigureAwait(true);
        this.RefreshExporter();
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private void RefreshExporter()
    {
        ConversationExportService service = this.exportService ?? new ConversationExportService();
        Conversation snapshot = this.EnsureConversation() with
        {
            Messages = this.Messages.Select(message => message.ToMessage()).ToImmutableArray(),
        };
        this.Exporter = new ExporterViewModel(service, this.shareService, snapshot);
    }

    private Conversation EnsureConversation()
    {
        if (this.conversation is not null)
        {
            return this.conversation;
        }

        this.conversation = new Conversation(
            this.ConversationId,
            ConversationTitler.DefaultTopic,
            string.Empty,
            ImmutableArray<Message>.Empty,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            new Mask(
                "default",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                "1f600",
                "Default",
                false,
                ImmutableArray<Message>.Empty,
                true,
                ModelConfig.NextChatDefault,
                "en",
                false,
                ImmutableArray<string>.Empty));
        return this.conversation;
    }

    private ModelConfig GetModelConfig()
    {
        return this.EnsureConversation().Mask.ModelConfig;
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private async Task RefreshConversationMetadataAsync(CancellationToken cancellationToken)
    {
        if (this.conversationRepository is null)
        {
            return;
        }

        Conversation current = this.EnsureConversation();
        bool changed = false;
        if (this.conversationTitler is not null && ConversationTitler.IsDefaultTopic(current.Topic))
        {
            try
            {
                string title = await this.conversationTitler.GenerateTitleAsync(current, cancellationToken).ConfigureAwait(true);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    current = current with
                    {
                        Topic = title,
                        LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    };
                    changed = true;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.StatusMessage = exception.Message;
            }
        }

        if (this.contextSummarizer is not null)
        {
            try
            {
                Conversation summarized = await this.contextSummarizer
                    .SummarizeAsync(current, CreateModelDescriptor(current.Mask.ModelConfig), cancellationToken)
                    .ConfigureAwait(true);
                if (!Equals(summarized, current))
                {
                    current = summarized;
                    changed = true;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.StatusMessage = exception.Message;
            }
        }

        if (changed)
        {
            this.conversation = current;
            await this.conversationRepository.UpsertAsync(current, cancellationToken).ConfigureAwait(true);
        }
    }
}
