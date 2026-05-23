// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using ImageMagick;

namespace ArcChat.Desktop.Features.Conversations;

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:ElementsMustAppearInTheCorrectOrder", Justification = "Static helpers stay grouped before instance attachment flow.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Public attachment flow stays before private compression helpers.")]
internal sealed class ImageAttachmentService
{
    public const int MaxImages = 3;
    public const int DefaultTargetBytes = 256 * 1024;

    private static readonly string[] HeicBrands = { "heic", "heix", "hevc", "hevx", "mif1", "msf1" };
    private readonly IImageAttachmentCache cache;
    private readonly IImageConverter converter;

    public static ImageAttachmentService CreateDefault()
    {
        return new ImageAttachmentService(new ImageAttachmentCache(), new MagickImageConverter());
    }

    public static bool IsHeic(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12 || !bytes.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            return false;
        }

        string brand = Encoding.ASCII.GetString(bytes.Slice(8, 4));
        return HeicBrands.Contains(brand, StringComparer.Ordinal);
    }

    public static string GuessMimeTypeFromName(string name)
    {
        string extension = Path.GetExtension(name);
        if (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase))
        {
            return "image/heic";
        }

        if (extension.Equals(".heif", StringComparison.OrdinalIgnoreCase))
        {
            return "image/heif";
        }

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return "image/jpeg";
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return "image/webp";
        }

        if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            return "image/bmp";
        }

        if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            return "image/gif";
        }

        return "application/octet-stream";
    }

    public static bool IsImageName(string name)
    {
        string mimeType = GuessMimeTypeFromName(name);
        return mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public ImageAttachmentService(IImageAttachmentCache cache, IImageConverter converter)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public async Task<ChatAttachment> CreateAttachmentAsync(
        byte[] bytes,
        string mimeType,
        int targetBytes = DefaultTargetBytes,
        bool compressionEnabled = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        if (targetBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetBytes), "Target size must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        PreparedImage prepared = await this.PrepareImageAsync(bytes, mimeType, cancellationToken).ConfigureAwait(false);
        byte[] payload = prepared.Payload;
        string payloadMimeType = prepared.MimeType;
        if (compressionEnabled)
        {
            if (prepared.HeicConverted && IsTargetSizedDataUrl(payloadMimeType, payload, targetBytes))
            {
                // NC04 calls out HEIC-to-PNG explicitly; keep that round-trip when it already fits.
            }
            else
            {
                payload = await Task.Run(
                    () => CompressToJpeg(payload, targetBytes, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                payloadMimeType = "image/jpeg";
            }
        }

        string id = Guid.NewGuid().ToString("N");
        string dataUrl = CreateDataUrl(payloadMimeType, payload);
        ChatAttachment attachment = new ChatAttachment(
            id,
            payloadMimeType,
            payload,
            dataUrl,
            "arcchat-cache://images/" + id);
        this.cache.Store(attachment);
        return attachment;
    }

    private static bool IsTargetSizedDataUrl(string mimeType, byte[] payload, int targetBytes)
    {
        return CreateDataUrl(mimeType, payload).Length <= targetBytes;
    }

    private static string CreateDataUrl(string mimeType, byte[] payload)
    {
        return "data:" + mimeType + ";base64," + Convert.ToBase64String(payload);
    }

    private static byte[] CompressToJpeg(byte[] payload, int targetBytes, CancellationToken cancellationToken)
    {
        using MagickImage image = new MagickImage(payload);
        image.AutoOrient();
        image.Strip();
        image.Format = MagickFormat.Jpeg;

        byte[] best = Array.Empty<byte>();
        for (int quality = 90, attempt = 0; attempt < 64; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            image.Quality = (uint)quality;
            using MemoryStream stream = new MemoryStream();
            image.Write(stream, MagickFormat.Jpeg);
            best = stream.ToArray();
            if (IsTargetSizedDataUrl("image/jpeg", best, targetBytes))
            {
                return best;
            }

            if (quality > 50)
            {
                quality -= 10;
                continue;
            }

            if (image.Width <= 1 && image.Height <= 1)
            {
                break;
            }

            uint nextWidth = Math.Max(1u, (uint)Math.Round(image.Width * 0.9d));
            uint nextHeight = Math.Max(1u, (uint)Math.Round(image.Height * 0.9d));
            image.Resize(nextWidth, nextHeight);
        }

        return best;
    }

    private static string NormalizeMimeType(string mimeType, ReadOnlySpan<byte> bytes)
    {
        if (IsHeic(bytes))
        {
            return "image/heic";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
        {
            return "image/bmp";
        }

        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return mimeType;
        }

        return mimeType;
    }

    private async Task<PreparedImage> PrepareImageAsync(byte[] bytes, string mimeType, CancellationToken cancellationToken)
    {
        string normalizedMimeType = NormalizeMimeType(mimeType, bytes);
        if (!IsHeic(bytes))
        {
            return new PreparedImage(bytes, normalizedMimeType, false);
        }

        byte[] pngBytes = await this.converter.HeicToPngAsync(bytes, cancellationToken).ConfigureAwait(false);
        return new PreparedImage(pngBytes, "image/png", true);
    }

    private sealed class PreparedImage
    {
        public PreparedImage(byte[] payload, string mimeType, bool heicConverted)
        {
            this.Payload = payload;
            this.MimeType = mimeType;
            this.HeicConverted = heicConverted;
        }

        public byte[] Payload { get; }

        public string MimeType { get; }

        public bool HeicConverted { get; }
    }
}
