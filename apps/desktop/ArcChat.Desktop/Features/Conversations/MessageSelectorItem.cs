// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.ViewModels;
using ArcChat.Protocol.Chat;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class MessageSelectorItem : ViewModelBase
{
    private bool isSelected;

    public MessageSelectorItem(Message message, int index, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(message);
        this.Message = message;
        this.Index = index;
        this.isSelected = isSelected;
        this.Text = MessageText.Extract(message);
    }

    public Message Message { get; }

    public int Index { get; }

    public string Text { get; }

    public string RoleName => this.Message.Role.ToString();

    public string Date => this.Message.Date;

    public string Preview => this.Text.Length <= 160 ? this.Text : this.Text[..160] + "...";

    public bool IsSelected
    {
        get => this.isSelected;
        set => this.SetProperty(ref this.isSelected, value);
    }
}
