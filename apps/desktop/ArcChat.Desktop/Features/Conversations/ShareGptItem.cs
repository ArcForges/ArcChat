// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ShareGptItem
{
    public ShareGptItem(string from, string value)
    {
        this.From = from;
        this.Value = value;
    }

    public string From { get; }

    public string Value { get; }
}
