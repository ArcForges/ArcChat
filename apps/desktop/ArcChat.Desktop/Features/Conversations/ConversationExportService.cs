// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Serialization;
using ArcChat.UI.Markdown.Markdown.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace ArcChat.Desktop.Features.Conversations;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Export service stays injectable for feature wiring and tests.")]
internal sealed class ConversationExportService
{
    private const char LineFeed = '\n';
    private const string NextChatShareMarker = "Share from [NextChat]: https://github.com/Yidadaa/ChatGPT-Next-Web";
    private static readonly Vector FixedDpi = new Vector(96, 96);
    private readonly MarkdownTextRenderer markdownTextRenderer;

    public ConversationExportService()
        : this(new MarkdownTextRenderer())
    {
    }

    internal ConversationExportService(MarkdownTextRenderer markdownTextRenderer)
    {
        this.markdownTextRenderer = markdownTextRenderer ?? throw new ArgumentNullException(nameof(markdownTextRenderer));
    }

    public ConversationExportDto CreateDto(Conversation conversation, IEnumerable<Message> selectedMessages, DateTimeOffset exportedAt)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(selectedMessages);
        return new ConversationExportDto(
            conversation.Id,
            conversation.Topic,
            conversation.MemoryPrompt,
            selectedMessages.ToImmutableArray(),
            exportedAt,
            conversation.ClearContextIndex);
    }

    public string CreateMarkdown(string topic, IEnumerable<Message> messages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(messages);
        StringBuilder builder = new StringBuilder();
        _ = builder.Append("# ").Append(topic.Trim()).Append(LineFeed);

        foreach (Message message in messages)
        {
            string label = message.Role switch
            {
                MessageRole.User => "You",
                MessageRole.Assistant => "ChatGPT",
                MessageRole.System => "System",
                _ => message.Role.ToString(),
            };
            string text = this.NormalizeLineEndings(this.markdownTextRenderer.Render(MessageText.Extract(message)));
            _ = builder.Append(LineFeed);
            _ = builder.Append("## ").Append(label).Append(':').Append(LineFeed);
            _ = builder.Append(text).Append(LineFeed);
        }

        return builder.ToString().TrimEnd() + LineFeed;
    }

    public string CreateJson(ConversationExportDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return this.NormalizeLineEndings(JsonSerializer.Serialize(dto, ArcChatProtocolJsonContext.Default.ConversationExportDto));
    }

    public ShareGptRequest CreateShareGptRequest(IEnumerable<Message> messages, string? avatarUrl = null)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ImmutableArray<ShareGptItem>.Builder builder = ImmutableArray.CreateBuilder<ShareGptItem>();
        foreach (Message message in messages)
        {
            builder.Add(new ShareGptItem(
                message.Role == MessageRole.User ? "human" : "gpt",
                MessageText.Extract(message)));
        }

        builder.Add(new ShareGptItem("human", NextChatShareMarker));
        return new ShareGptRequest(avatarUrl, builder.ToImmutable());
    }

    public string CreateLocalShareUri(ConversationExportDto dto)
    {
        string json = this.CreateJson(dto);
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "arcchat://share/" + payload;
    }

    public bool TryParseLocalShareUri(string value, [NotNullWhen(true)] out ConversationExportDto? dto)
    {
        dto = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Scheme, "arcchat", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "share", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string payload = uri.AbsolutePath.Trim('/');
        if (payload.Length == 0)
        {
            return false;
        }

        string base64 = payload.Replace('-', '+').Replace('_', '/');
        int padding = (4 - (base64.Length % 4)) % 4;
        base64 = base64.PadRight(base64.Length + padding, '=');
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            dto = JsonSerializer.Deserialize(
                Encoding.UTF8.GetString(bytes),
                ArcChatProtocolJsonContext.Default.ConversationExportDto);
            return dto is not null;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public byte[] CaptureChatImage(Control chatVisualTree, PixelSize pixelSize)
    {
        ArgumentNullException.ThrowIfNull(chatVisualTree);
        using RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, FixedDpi);
        Size size = new Size(pixelSize.Width, pixelSize.Height);
        chatVisualTree.Measure(size);
        chatVisualTree.Arrange(new Rect(size));
        bitmap.Render(chatVisualTree);
        using MemoryStream stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }

    private string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}
