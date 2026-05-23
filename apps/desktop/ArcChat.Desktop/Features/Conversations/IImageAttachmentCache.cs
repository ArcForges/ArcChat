// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal interface IImageAttachmentCache
{
    void Store(ChatAttachment attachment);

    bool TryGet(string cacheKey, out ChatAttachment attachment);
}
