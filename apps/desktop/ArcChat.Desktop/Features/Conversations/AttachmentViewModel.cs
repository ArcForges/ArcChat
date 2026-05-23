// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Desktop.ViewModels;
using Avalonia.Media.Imaging;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class AttachmentViewModel : ViewModelBase, IDisposable
{
    private AttachmentViewModel(ChatAttachment attachment, Bitmap? preview)
    {
        this.Id = attachment.Id;
        this.MimeType = attachment.MimeType;
        this.DataUrl = attachment.DataUrl;
        this.ByteCount = attachment.ByteCount;
        this.Preview = preview;
    }

    public string Id { get; }

    public string MimeType { get; }

    public string DataUrl { get; }

    public int ByteCount { get; }

    public Bitmap? Preview { get; }

    public string Label => this.MimeType + " " + FormatBytes(this.ByteCount);

    public static AttachmentViewModel FromAttachment(ChatAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return new AttachmentViewModel(attachment, CreatePreview(attachment.Payload));
    }

    public void Dispose()
    {
        this.Preview?.Dispose();
    }

    private static Bitmap? CreatePreview(byte[] payload)
    {
        try
        {
            return new Bitmap(new MemoryStream(payload));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string FormatBytes(int bytes)
    {
        double kib = bytes / 1024d;
        return kib < 1024d
            ? kib.ToString("0.#", CultureInfo.InvariantCulture) + " KB"
            : (kib / 1024d).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
    }
}
