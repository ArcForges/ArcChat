// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.ViewModels;
using Avalonia.Media.Imaging;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class MessageImageViewModel : ViewModelBase, IDisposable
{
    private MessageImageViewModel(string url, Bitmap? preview)
    {
        this.Url = url;
        this.Preview = preview;
    }

    public string Url { get; }

    public Bitmap? Preview { get; }

    public string Label => this.Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
        ? "Attached image"
        : this.Url;

    public static MessageImageViewModel FromUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new MessageImageViewModel(url, TryCreatePreview(url));
    }

    public void Dispose()
    {
        this.Preview?.Dispose();
    }

    private static Bitmap? TryCreatePreview(string url)
    {
        if (!url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int commaIndex = url.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0)
        {
            return null;
        }

        try
        {
            int payloadStart = commaIndex + 1;
            byte[] bytes = Convert.FromBase64String(url[payloadStart..]);
            return new Bitmap(new MemoryStream(bytes));
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
}
