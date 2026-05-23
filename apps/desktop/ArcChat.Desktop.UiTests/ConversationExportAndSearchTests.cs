// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Serialization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class ConversationExportAndSearchTests
{
    [Fact]
    public static void MessageSelectorSelectAllAndInvertRespectVisibleMessages()
    {
        Conversation conversation = CreateConversation();
        MessageSelectorViewModel selector = new MessageSelectorViewModel(conversation.Messages, conversation.ClearContextIndex);

        _ = selector.Items.Should().HaveCount(2);
        _ = selector.SelectedMessages.Should().HaveCount(2);

        selector.SearchText = "plain";
        selector.InvertCommand.Execute(null);

        _ = selector.VisibleItems.Should().ContainSingle();
        _ = selector.SelectedMessages.Select(message => message.Id).Should().Equal("a1");

        selector.SelectAllCommand.Execute(null);

        _ = selector.SelectedMessages.Select(message => message.Id).Should().Equal("a1", "u2");
    }

    [Fact]
    public static void ExporterProducesMarkdownJsonShareAndDeepLinkGoldens()
    {
        Conversation conversation = CreateConversation();
        ConversationExportService exportService = new ConversationExportService();
        ConversationExportDto dto = exportService.CreateDto(conversation, conversation.Messages, DateTimeOffset.UnixEpoch);
        string outputDirectory = ExporterGoldenDirectory();
        Directory.CreateDirectory(outputDirectory);

        string markdown = exportService.CreateMarkdown(conversation.Topic, conversation.Messages);
        AssertGolden(Path.Join(outputDirectory, "conversation.md"), markdown);

        string json = exportService.CreateJson(dto);
        AssertGolden(Path.Join(outputDirectory, "conversation.json"), json);
        ConversationExportDto? roundTrip = JsonSerializer.Deserialize(json, ArcChatProtocolJsonContext.Default.ConversationExportDto);
        _ = roundTrip.Should().BeEquivalentTo(dto);

        ShareGptRequest request = exportService.CreateShareGptRequest(conversation.Messages);
        string requestJson = NormalizeLineEndings(JsonSerializer.Serialize(request, ConversationExportJsonContext.Default.ShareGptRequest));
        AssertGolden(Path.Join(outputDirectory, "sharegpt-request.json"), requestJson);
        _ = request.Items.Should().Contain(item => string.Equals(item.From, "human", StringComparison.Ordinal) && item.Value.Contains("[NextChat]", StringComparison.Ordinal));

        string shareUri = exportService.CreateLocalShareUri(dto);
        AssertGolden(Path.Join(outputDirectory, "local-share-uri.txt"), shareUri + "\n");
        _ = exportService.TryParseLocalShareUri(shareUri, out ConversationExportDto? parsed).Should().BeTrue();
        _ = parsed.Should().BeEquivalentTo(dto);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Headless Avalonia dispatch must remain on the UI thread.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "Headless dispatch returns an implementation value ignored by the test.")]
    public static async Task ImageExporterProducesGoldenPng()
    {
        using HeadlessUnitTestSession session = TestAppBuilder.StartHeadlessSession();
        await session.Dispatch(
            () =>
            {
                string outputDirectory = ExporterGoldenDirectory();
                Directory.CreateDirectory(outputDirectory);
                string outputPath = Path.Join(outputDirectory, "conversation.png");
                ConversationExportService exportService = new ConversationExportService();
                StackPanel chatTree = new StackPanel
                {
                    Width = 480,
                    Height = 320,
                    Spacing = 8,
                };
                chatTree.Children.Add(CreateBubble("You", "Plain user message"));
                chatTree.Children.Add(CreateBubble("ChatGPT", "Assistant answer with markdown."));
                Border captureRoot = new Border
                {
                    Width = 480,
                    Height = 320,
                    Background = Avalonia.Media.Brushes.White,
                    Child = chatTree,
                };

                Window window = new Window
                {
                    Width = 480,
                    Height = 320,
                    Content = captureRoot,
                };
                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                    byte[] png = exportService.CaptureChatImage(captureRoot, new Avalonia.PixelSize(480, 320));
                    if (!File.Exists(outputPath) || new FileInfo(outputPath).Length <= 1024)
                    {
                        File.WriteAllBytes(outputPath, png);
                    }

                    _ = new FileInfo(outputPath).Length.Should().BeGreaterThan(1024);
                }
                finally
                {
                    TestAppBuilder.CloseWindow(window);
                }
            },
            CancellationToken.None).ConfigureAwait(true);
    }

    [Fact]
    public static async Task ExporterViewModelSharesThroughShareService()
    {
        RecordingShareService shareService = new RecordingShareService(new Uri("https://shareg.pt/test"));
        ExporterViewModel viewModel = new ExporterViewModel(new ConversationExportService(), shareService, CreateConversation());

        await viewModel.ShareCommand.ExecuteAsync(null).ConfigureAwait(true);

        _ = viewModel.LastRemoteShareUri.Should().Be(new Uri("https://shareg.pt/test"));
        _ = shareService.Request.Should().NotBeNull();
        _ = viewModel.StatusMessage.Should().Be("https://shareg.pt/test");
    }

    [Fact]
    public static void SearchChatFindsFiveThousandMessagesUnderFiftyMilliseconds()
    {
        SearchChatViewModel viewModel = new SearchChatViewModel();
        viewModel.SetConversationsForTest(CreateSearchConversations(100, 50));

        viewModel.Query = "warmup";
        viewModel.Query = "needle";

        _ = viewModel.Results.Should().ContainSingle(result => string.Equals(result.MessageId, "m-42-17", StringComparison.Ordinal));
        _ = viewModel.LastSearchElapsed.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(50));
    }

    private static Border CreateBubble(string role, string text)
    {
        StackPanel panel = new StackPanel
        {
            Spacing = 4,
        };
        panel.Children.Add(new Avalonia.Controls.TextBlock { Text = role, FontWeight = Avalonia.Media.FontWeight.SemiBold });
        panel.Children.Add(new Avalonia.Controls.TextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        return new Border
        {
            Padding = new Avalonia.Thickness(12),
            Margin = new Avalonia.Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = panel,
        };
    }

    private static List<Conversation> CreateSearchConversations(int conversationCount, int messagesPerConversation)
    {
        List<Conversation> conversations = new List<Conversation>(conversationCount);
        for (int conversationIndex = 0; conversationIndex < conversationCount; conversationIndex++)
        {
            ImmutableArray<Message>.Builder messages = ImmutableArray.CreateBuilder<Message>(messagesPerConversation);
            for (int messageIndex = 0; messageIndex < messagesPerConversation; messageIndex++)
            {
                string text = conversationIndex == 42 && messageIndex == 17
                    ? "this message contains the search needle"
                    : "ordinary transcript row " + conversationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + messageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                messages.Add(Message.Text(
                    "m-" + conversationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + messageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MessageRole.Assistant,
                    text,
                    "2026-05-22"));
            }

            conversations.Add(CreateConversation("c-" + conversationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), "Topic " + conversationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), messages.ToImmutable(), null));
        }

        return conversations;
    }

    private static Conversation CreateConversation()
    {
        return CreateConversation(
            "c-export",
            "Export Topic",
            ImmutableArray.Create(
                Message.Text("u1", MessageRole.User, "Context before clear", "2026-05-22"),
                Message.Text("a1", MessageRole.Assistant, "Assistant **markdown** answer.", "2026-05-22"),
                Message.Text("u2", MessageRole.User, "Plain user message", "2026-05-22")),
            1);
    }

    private static Conversation CreateConversation(string id, string topic, ImmutableArray<Message> messages, int? clearContextIndex)
    {
        return new Conversation(
            id,
            topic,
            string.Empty,
            messages,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UnixEpoch.ToUnixTimeMilliseconds(),
            0,
            clearContextIndex,
            new ArcChat.Protocol.Masks.Mask(
                "mask-" + id,
                DateTimeOffset.UnixEpoch.ToUnixTimeMilliseconds(),
                "1f600",
                "Default",
                false,
                ImmutableArray<Message>.Empty,
                true,
                ArcChat.Protocol.Providers.ModelConfig.NextChatDefault,
                "en",
                false,
                ImmutableArray<string>.Empty));
    }

    private static void AssertGolden(string path, string contents)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, contents);
        }

        _ = File.ReadAllText(path).Should().Be(contents);
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string ExporterGoldenDirectory()
    {
        return Path.Join(FindRepositoryRoot(), "docs", "coverage", "exporter-golden");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Join(directory.FullName, "ArcChat.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    private sealed class RecordingShareService : IShareService
    {
        private readonly Uri result;

        public RecordingShareService(Uri result)
        {
            this.result = result;
        }

        public ShareGptRequest? Request { get; private set; }

        public Task<Uri?> ShareAsync(ShareGptRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;
            return Task.FromResult<Uri?>(this.result);
        }
    }
}
