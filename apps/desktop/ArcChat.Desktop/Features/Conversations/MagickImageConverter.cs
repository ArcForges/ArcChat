// Copyright (c) ArcForges. Licensed under the MIT License.

using ImageMagick;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class MagickImageConverter : IImageConverter
{
    public Task<byte[]> HeicToPngAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using MagickImage image = new MagickImage(bytes);
                image.AutoOrient();
                image.Strip();
                image.Format = MagickFormat.Png;
                using MemoryStream stream = new MemoryStream();
                image.Write(stream, MagickFormat.Png);
                return stream.ToArray();
            },
            cancellationToken);
    }
}
