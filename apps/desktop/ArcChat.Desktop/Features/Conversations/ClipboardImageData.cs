// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ClipboardImageData
{
    public ClipboardImageData(byte[] bytes, string mimeType)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        this.Bytes = bytes;
        this.MimeType = mimeType;
    }

    public byte[] Bytes { get; }

    public string MimeType { get; }
}
