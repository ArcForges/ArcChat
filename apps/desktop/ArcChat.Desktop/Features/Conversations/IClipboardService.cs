// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal interface IClipboardService
{
    Task<ClipboardImageData?> GetImageAsync(CancellationToken cancellationToken = default);
}
