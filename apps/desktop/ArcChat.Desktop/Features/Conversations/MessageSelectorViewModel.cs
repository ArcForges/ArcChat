// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using ArcChat.Desktop.ViewModels;
using ArcChat.Protocol.Chat;
using ArcChat.UI.Controls.Search;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class MessageSelectorViewModel : ViewModelBase
{
    private string searchText = string.Empty;

    public MessageSelectorViewModel()
        : this(CreateDesignMessages(), null)
    {
    }

    public MessageSelectorViewModel(IEnumerable<Message> messages, int? clearContextIndex)
    {
        ArgumentNullException.ThrowIfNull(messages);
        this.SelectAllCommand = new RelayCommand(this.SelectAll);
        this.InvertCommand = new RelayCommand(this.InvertSelection);

        Message[] messageArray = messages.ToArray();
        int startIndex = clearContextIndex is { } boundary && boundary >= 0 && boundary < messageArray.Length
            ? boundary
            : 0;

        for (int index = startIndex; index < messageArray.Length; index++)
        {
            Message message = messageArray[index];
            if (IsSelectable(message))
            {
                this.Items.Add(new MessageSelectorItem(message, index, true));
            }
        }

        this.RefreshVisibleItems();
    }

    public ObservableCollection<MessageSelectorItem> Items { get; } = new ObservableCollection<MessageSelectorItem>();

    public ObservableCollection<MessageSelectorItem> VisibleItems { get; } = new ObservableCollection<MessageSelectorItem>();

    public IRelayCommand SelectAllCommand { get; }

    public IRelayCommand InvertCommand { get; }

    public string SearchText
    {
        get => this.searchText;
        set
        {
            if (this.SetProperty(ref this.searchText, value))
            {
                this.RefreshVisibleItems();
            }
        }
    }

    public IReadOnlyList<Message> SelectedMessages => this.Items
        .Where(item => item.IsSelected)
        .Select(item => item.Message)
        .ToArray();

    public void SelectAll()
    {
        foreach (MessageSelectorItem item in this.VisibleItems)
        {
            item.IsSelected = true;
        }
    }

    public void InvertSelection()
    {
        foreach (MessageSelectorItem item in this.VisibleItems)
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    private static bool IsSelectable(Message message)
    {
        return !message.Streaming && !message.IsError && MessageText.Extract(message).Length > 0;
    }

    private static ImmutableArray<Message> CreateDesignMessages()
    {
        return ImmutableArray.Create(
            Message.Text("design-u", MessageRole.User, "How should we export this?", "2026-05-22"),
            Message.Text("design-a", MessageRole.Assistant, "Use Markdown, JSON, image, or ShareGPT.", "2026-05-22"));
    }

    private void RefreshVisibleItems()
    {
        this.VisibleItems.Clear();
        string query = this.SearchText.Trim();
        foreach (MessageSelectorItem item in this.Items.Where(item =>
            query.Length == 0 || FuzzyMatcher.Match(item.Text.AsSpan(), query.AsSpan()).IsMatch))
        {
            this.VisibleItems.Add(item);
        }
    }
}
