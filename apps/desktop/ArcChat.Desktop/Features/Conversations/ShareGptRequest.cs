// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ShareGptRequest
{
    public ShareGptRequest(string? avatarUrl, ImmutableArray<ShareGptItem> items)
    {
        this.AvatarUrl = avatarUrl;
        this.Items = items;
    }

    public string? AvatarUrl { get; }

    public ImmutableArray<ShareGptItem> Items { get; }
}
