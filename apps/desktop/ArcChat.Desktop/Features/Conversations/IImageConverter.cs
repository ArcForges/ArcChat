// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal interface IImageConverter
{
    Task<byte[]> HeicToPngAsync(byte[] bytes, CancellationToken cancellationToken = default);
}
