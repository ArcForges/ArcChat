// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly TopLevel? topLevel;

    public AvaloniaClipboardService(TopLevel? topLevel)
    {
        this.topLevel = topLevel;
    }

    public async Task<ClipboardImageData?> GetImageAsync(CancellationToken cancellationToken = default)
    {
        if (this.topLevel?.Clipboard is not { } clipboard)
        {
            return null;
        }

        IAsyncDataTransfer? dataTransfer = await clipboard.TryGetDataAsync().ConfigureAwait(true);
        if (dataTransfer is null)
        {
            return null;
        }

        using IDisposable? dataTransferDisposable = dataTransfer as IDisposable;

        cancellationToken.ThrowIfCancellationRequested();
        Bitmap? bitmap = await dataTransfer.TryGetBitmapAsync().ConfigureAwait(true);
        if (bitmap is null)
        {
            return null;
        }

        using (bitmap)
        {
            using MemoryStream stream = new MemoryStream();
            bitmap.Save(stream);
            return new ClipboardImageData(stream.ToArray(), "image/png");
        }
    }
}
