// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence.Repositories;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ConversationListViewModel : ViewModelBase
{
    private readonly IConversationRepository? repository;
    private ConversationListItem? selectedConversation;
    private bool hasConversations;
    private bool hasNoConversations = true;
    private string statusMessage = string.Empty;

    public ConversationListViewModel()
        : this(null, true)
    {
    }

    internal ConversationListViewModel(IConversationRepository repository)
        : this(repository, false)
    {
    }

    private ConversationListViewModel(IConversationRepository? repository, bool seedDesignItems)
    {
        this.repository = repository;
        this.Conversations.CollectionChanged += this.OnConversationsChanged;
        this.LoadCommand = new AsyncRelayCommand(this.LoadAsync);
        this.PinCommand = new AsyncRelayCommand<ConversationListItem>(this.PinAsync);
        this.UnpinCommand = new AsyncRelayCommand<ConversationListItem>(this.UnpinAsync);
        this.ArchiveCommand = new AsyncRelayCommand<ConversationListItem>(this.ArchiveAsync);
        this.UnarchiveCommand = new AsyncRelayCommand<ConversationListItem>(this.UnarchiveAsync);
        this.DeleteCommand = new AsyncRelayCommand<ConversationListItem>(this.DeleteAsync);
        this.ReorderCommand = new AsyncRelayCommand<ConversationListMoveRequest>(this.ReorderAsync);
        this.BeginRenameCommand = new RelayCommand<ConversationListItem>(this.BeginRename);
        this.CommitRenameCommand = new AsyncRelayCommand<ConversationListItem>(this.CommitRenameAsync);
        this.CancelRenameCommand = new RelayCommand<ConversationListItem>(this.CancelRename);

        if (seedDesignItems)
        {
            this.Conversations.Add(new ConversationListItem("design-1", "Local chat", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), true, false, 0, 2));
            this.Conversations.Add(new ConversationListItem("design-2", "Provider notes", DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeMilliseconds(), false, false, 1, 0));
            this.RefreshConversationState();
        }
    }

    public ObservableCollection<ConversationListItem> Conversations { get; } = new ObservableCollection<ConversationListItem>();

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> PinCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> UnpinCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> ArchiveCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> UnarchiveCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> DeleteCommand { get; }

    public IAsyncRelayCommand<ConversationListMoveRequest> ReorderCommand { get; }

    public IRelayCommand<ConversationListItem> BeginRenameCommand { get; }

    public IAsyncRelayCommand<ConversationListItem> CommitRenameCommand { get; }

    public IRelayCommand<ConversationListItem> CancelRenameCommand { get; }

    public ConversationListItem? SelectedConversation
    {
        get => this.selectedConversation;
        set => this.SetProperty(ref this.selectedConversation, value);
    }

    public bool HasConversations
    {
        get => this.hasConversations;
        private set => this.SetProperty(ref this.hasConversations, value);
    }

    public bool HasNoConversations
    {
        get => this.hasNoConversations;
        private set => this.SetProperty(ref this.hasNoConversations, value);
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (this.repository is null)
        {
            this.RefreshConversationState();
            return;
        }

        IReadOnlyList<ConversationListEntry> entries = await this.repository.ListEntriesAsync(false, cancellationToken).ConfigureAwait(true);
        this.Conversations.Clear();
        foreach (ConversationListEntry entry in entries)
        {
            this.Conversations.Add(new ConversationListItem(
                entry.Id,
                entry.Topic,
                entry.UpdatedAt,
                entry.IsPinned,
                entry.IsArchived,
                entry.SortOrder,
                entry.UnreadCount));
        }

        this.RefreshConversationState();
        this.StatusMessage = string.Empty;
    }

    internal async Task MoveAsync(string conversationId, int targetIndex, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        int sourceIndex = this.IndexOf(conversationId);
        if (sourceIndex < 0 || this.Conversations.Count == 0)
        {
            return;
        }

        int boundedTarget = Math.Clamp(targetIndex, 0, this.Conversations.Count - 1);
        if (sourceIndex == boundedTarget)
        {
            return;
        }

        ConversationListItem item = this.Conversations[sourceIndex];
        this.Conversations.RemoveAt(sourceIndex);
        if (boundedTarget > this.Conversations.Count)
        {
            boundedTarget = this.Conversations.Count;
        }

        this.Conversations.Insert(boundedTarget, item);
        await this.PersistOrderAsync(cancellationToken).ConfigureAwait(true);
        await this.LoadAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task PinAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await this.SetPinnedAsync(item, true, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task UnpinAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await this.SetPinnedAsync(item, false, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task SetPinnedAsync(ConversationListItem item, bool isPinned, CancellationToken cancellationToken)
    {
        if (this.repository is not null)
        {
            await this.repository.SetPinnedAsync(item.Id, isPinned, cancellationToken).ConfigureAwait(true);
            await this.LoadAsync(cancellationToken).ConfigureAwait(true);
            return;
        }

        item.IsPinned = isPinned;
        this.ApplyInMemorySort();
    }

    private async Task ArchiveAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await this.SetArchivedAsync(item, true, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task UnarchiveAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await this.SetArchivedAsync(item, false, CancellationToken.None).ConfigureAwait(true);
    }

    private async Task SetArchivedAsync(ConversationListItem item, bool isArchived, CancellationToken cancellationToken)
    {
        if (this.repository is not null)
        {
            await this.repository.SetArchivedAsync(item.Id, isArchived, cancellationToken).ConfigureAwait(true);
            await this.LoadAsync(cancellationToken).ConfigureAwait(true);
            return;
        }

        item.IsArchived = isArchived;
        if (isArchived)
        {
            _ = this.Conversations.Remove(item);
        }
    }

    private async Task DeleteAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (this.repository is not null)
        {
            await this.repository.DeleteAsync(item.Id, CancellationToken.None).ConfigureAwait(true);
        }

        _ = this.Conversations.Remove(item);
    }

    private Task ReorderAsync(ConversationListMoveRequest? request)
    {
        return request is null
            ? Task.CompletedTask
            : this.MoveAsync(request.ConversationId, request.TargetIndex, CancellationToken.None);
    }

    private void BeginRename(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.DraftTitle = item.Title;
        item.IsEditing = true;
    }

    private async Task CommitRenameAsync(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        string nextTitle = item.DraftTitle.Trim();
        if (nextTitle.Length == 0)
        {
            this.CancelRename(item);
            return;
        }

        if (this.repository is not null)
        {
            await this.repository.RenameAsync(item.Id, nextTitle, CancellationToken.None).ConfigureAwait(true);
        }

        item.Title = nextTitle;
        item.IsEditing = false;
    }

    private void CancelRename(ConversationListItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.DraftTitle = item.Title;
        item.IsEditing = false;
    }

    private async Task PersistOrderAsync(CancellationToken cancellationToken)
    {
        for (int index = 0; index < this.Conversations.Count; index++)
        {
            this.Conversations[index].SortOrder = index;
        }

        if (this.repository is not null)
        {
            await this.repository.ReorderAsync(this.Conversations.Select(item => item.Id).ToArray(), cancellationToken).ConfigureAwait(true);
        }
    }

    private int IndexOf(string conversationId)
    {
        for (int index = 0; index < this.Conversations.Count; index++)
        {
            if (string.Equals(this.Conversations[index].Id, conversationId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private void ApplyInMemorySort()
    {
        ConversationListItem[] ordered = this.Conversations
            .OrderByDescending(item => item.IsPinned)
            .ThenBy(item => item.SortOrder)
            .ThenByDescending(item => item.UpdatedAt)
            .ToArray();
        this.Conversations.Clear();
        foreach (ConversationListItem item in ordered)
        {
            this.Conversations.Add(item);
        }
    }

    private void OnConversationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RefreshConversationState();
    }

    private void RefreshConversationState()
    {
        this.HasConversations = this.Conversations.Count > 0;
        this.HasNoConversations = !this.HasConversations;
    }
}
