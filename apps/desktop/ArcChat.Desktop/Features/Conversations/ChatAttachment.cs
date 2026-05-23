// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class ChatAttachment
{
    public ChatAttachment(string id, string mimeType, byte[] payload, string dataUrl, string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        this.Id = id;
        this.MimeType = mimeType;
        this.Payload = payload;
        this.DataUrl = dataUrl;
        this.CacheKey = cacheKey;
    }

    public string Id { get; }

    public string MimeType { get; }

    public byte[] Payload { get; }

    public string DataUrl { get; }

    public string CacheKey { get; }

    public int ByteCount => this.Payload.Length;
}
