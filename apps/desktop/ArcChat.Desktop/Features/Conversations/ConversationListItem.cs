// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.ViewModels;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ConversationListItem : ViewModelBase
{
    private string title;
    private bool isPinned;
    private bool isArchived;
    private int unreadCount;
    private int sortOrder;
    private string draftTitle;
    private bool isEditing;

    public ConversationListItem(
        string id,
        string title,
        long updatedAt,
        bool isPinned,
        bool isArchived,
        int sortOrder,
        int unreadCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        this.Id = id;
        this.title = title;
        this.UpdatedAt = updatedAt;
        this.isPinned = isPinned;
        this.isArchived = isArchived;
        this.sortOrder = sortOrder;
        this.unreadCount = unreadCount;
        this.draftTitle = title;
    }

    public string Id { get; }

    public long UpdatedAt { get; }

    public string Title
    {
        get => this.title;
        set => this.SetProperty(ref this.title, value);
    }

    public bool IsPinned
    {
        get => this.isPinned;
        set
        {
            if (this.SetProperty(ref this.isPinned, value))
            {
                this.OnPropertyChanged(nameof(this.CanPin));
                this.OnPropertyChanged(nameof(this.CanUnpin));
            }
        }
    }

    public bool CanPin => !this.IsPinned;

    public bool CanUnpin => this.IsPinned;

    public bool IsArchived
    {
        get => this.isArchived;
        set
        {
            if (this.SetProperty(ref this.isArchived, value))
            {
                this.OnPropertyChanged(nameof(this.CanArchive));
                this.OnPropertyChanged(nameof(this.CanUnarchive));
            }
        }
    }

    public bool CanArchive => !this.IsArchived;

    public bool CanUnarchive => this.IsArchived;

    public int UnreadCount
    {
        get => this.unreadCount;
        set
        {
            if (this.SetProperty(ref this.unreadCount, value))
            {
                this.OnPropertyChanged(nameof(this.HasUnread));
            }
        }
    }

    public bool HasUnread => this.UnreadCount > 0;

    public int SortOrder
    {
        get => this.sortOrder;
        set => this.SetProperty(ref this.sortOrder, value);
    }

    public string DraftTitle
    {
        get => this.draftTitle;
        set => this.SetProperty(ref this.draftTitle, value);
    }

    public bool IsEditing
    {
        get => this.isEditing;
        set => this.SetProperty(ref this.isEditing, value);
    }

    public string UpdatedAtText
    {
        get
        {
            DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(this.UpdatedAt).ToLocalTime();
            return date.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
