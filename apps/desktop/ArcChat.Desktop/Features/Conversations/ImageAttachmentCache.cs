// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ImageAttachmentCache : IImageAttachmentCache
{
    private readonly Dictionary<string, ChatAttachment> attachments = new Dictionary<string, ChatAttachment>(StringComparer.Ordinal);

    public void Store(ChatAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        this.attachments[attachment.CacheKey] = attachment;
    }

    public bool TryGet(string cacheKey, out ChatAttachment attachment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        return this.attachments.TryGetValue(cacheKey, out attachment!);
    }
}
