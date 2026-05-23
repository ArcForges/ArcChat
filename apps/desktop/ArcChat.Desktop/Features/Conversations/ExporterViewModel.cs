// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ArcChat.Desktop.ViewModels;
using ArcChat.Protocol.Chat;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Conversations;

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsShouldAppearBeforeInstanceElements", Justification = "Design-data factory is kept near the bottom.")]
internal sealed class ExporterViewModel : ViewModelBase
{
    private readonly ConversationExportService exportService;
    private readonly IShareService? shareService;
    private readonly Conversation conversation;
    private ConversationExportFormat selectedFormat;
    private string previewText = string.Empty;
    private string localShareUri = string.Empty;
    private string statusMessage = string.Empty;
    private byte[] lastImageBytes = Array.Empty<byte>();
    private ShareGptRequest? lastShareRequest;
    private Uri? lastRemoteShareUri;

    public ExporterViewModel()
        : this(new ConversationExportService(), null, CreateDesignConversation())
    {
    }

    public ExporterViewModel(ConversationExportService exportService, IShareService? shareService, Conversation conversation)
    {
        this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        this.shareService = shareService;
        this.conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
        this.Selector = new MessageSelectorViewModel(conversation.Messages, conversation.ClearContextIndex);
        this.RefreshCommand = new RelayCommand(this.RefreshPreview);
        this.ShareCommand = new AsyncRelayCommand(this.ShareAsync);
        this.CaptureImageCommand = new RelayCommand<Control>(this.CaptureImage);
        this.SelectMarkdownCommand = new RelayCommand(() => this.SelectedFormat = ConversationExportFormat.Markdown);
        this.SelectJsonCommand = new RelayCommand(() => this.SelectedFormat = ConversationExportFormat.Json);
        this.SelectImageCommand = new RelayCommand(() => this.SelectedFormat = ConversationExportFormat.Image);
        this.RefreshPreview();
    }

    public MessageSelectorViewModel Selector { get; }

    public IRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand ShareCommand { get; }

    public IRelayCommand<Control> CaptureImageCommand { get; }

    public IRelayCommand SelectMarkdownCommand { get; }

    public IRelayCommand SelectJsonCommand { get; }

    public IRelayCommand SelectImageCommand { get; }

    public ConversationExportFormat SelectedFormat
    {
        get => this.selectedFormat;
        set
        {
            if (this.SetProperty(ref this.selectedFormat, value))
            {
                this.RefreshPreview();
            }
        }
    }

    public string PreviewText
    {
        get => this.previewText;
        private set => this.SetProperty(ref this.previewText, value);
    }

    public string LocalShareUri
    {
        get => this.localShareUri;
        private set => this.SetProperty(ref this.localShareUri, value);
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    public byte[] LastImageBytes
    {
        get => this.lastImageBytes;
        private set => this.SetProperty(ref this.lastImageBytes, value);
    }

    public ShareGptRequest? LastShareRequest
    {
        get => this.lastShareRequest;
        private set => this.SetProperty(ref this.lastShareRequest, value);
    }

    public Uri? LastRemoteShareUri
    {
        get => this.lastRemoteShareUri;
        private set => this.SetProperty(ref this.lastRemoteShareUri, value);
    }

    public ConversationExportDto CreateCurrentDto(DateTimeOffset exportedAt)
    {
        return this.exportService.CreateDto(this.conversation, this.Selector.SelectedMessages, exportedAt);
    }

    public void RefreshPreview()
    {
        IReadOnlyList<Message> messages = this.Selector.SelectedMessages;
        ConversationExportDto dto = this.CreateCurrentDto(DateTimeOffset.UnixEpoch);
        this.LocalShareUri = this.exportService.CreateLocalShareUri(dto);
        this.PreviewText = this.SelectedFormat switch
        {
            ConversationExportFormat.Json => this.exportService.CreateJson(dto),
            ConversationExportFormat.Image => "PNG image export",
            _ => this.exportService.CreateMarkdown(this.conversation.Topic, messages),
        };
        this.StatusMessage = messages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " selected";
    }

    private async Task ShareAsync()
    {
        ShareGptRequest request = this.exportService.CreateShareGptRequest(this.Selector.SelectedMessages);
        this.LastShareRequest = request;
        this.LastRemoteShareUri = this.shareService is null ? null : await this.shareService.ShareAsync(request, CancellationToken.None).ConfigureAwait(true);
        this.StatusMessage = this.LastRemoteShareUri?.ToString() ?? this.LocalShareUri;
    }

    private void CaptureImage(Control? control)
    {
        if (control is null)
        {
            return;
        }

        this.LastImageBytes = this.exportService.CaptureChatImage(control, new PixelSize(960, 720));
        this.StatusMessage = "Image captured";
    }

    private static Conversation CreateDesignConversation()
    {
        System.Collections.Immutable.ImmutableArray<Message> messages =
        [
            Message.Text("design-u", MessageRole.User, "Show **markdown** export.", "2026-05-22"),
            Message.Text("design-a", MessageRole.Assistant, "Rendered as plain text for portable files.", "2026-05-22"),
        ];

        return new Conversation(
            "design-export",
            "Export Preview",
            string.Empty,
            messages,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            new ArcChat.Protocol.Masks.Mask(
                "design-mask",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                "1f600",
                "Default",
                false,
                [],
                true,
                ArcChat.Protocol.Providers.ModelConfig.NextChatDefault,
                "en",
                false,
                []));
    }
}
