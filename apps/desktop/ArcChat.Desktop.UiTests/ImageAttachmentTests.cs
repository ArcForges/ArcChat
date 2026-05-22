// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ArcChat.Agent;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.LocalPersistence;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class ImageAttachmentTests
{
    private static readonly byte[] SmallImage = CreateBmp24(2, 2);

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait", Justification = "Async disposal belongs to the test scope.")]
    public static async Task AttachmentsRespectVisionGateAndMaxThree()
    {
        _ = NextChatVisionModelGate.IsVisionModel("gpt-3.5-turbo").Should().BeFalse();
        _ = NextChatVisionModelGate.IsVisionModel("gpt-4o-mini").Should().BeTrue();
        _ = NextChatVisionModelGate.IsVisionModel("claude-3-5-haiku-20241022").Should().BeFalse();

        ImageAttachmentService attachmentService = CreateAttachmentService();
        await using ArcChatDatabase nonVisionDatabase = await CreateDatabaseAsync("c-nonvision", "gpt-3.5-turbo").ConfigureAwait(true);
        ChatDetailViewModel nonVision = new ChatDetailViewModel(
            "c-nonvision",
            new CapturingAgentRuntime(),
            nonVisionDatabase.Conversations,
            nonVisionDatabase.Messages,
            imageAttachmentService: attachmentService);
        await nonVision.LoadAsync(CancellationToken.None).ConfigureAwait(true);

        await nonVision.AddImageBytesAsync(SmallImage, "image/bmp", CancellationToken.None).ConfigureAwait(true);

        _ = nonVision.Attachments.Should().BeEmpty();
        _ = nonVision.StatusMessage.Should().Be("Images require a vision model");

        await using ArcChatDatabase visionDatabase = await CreateDatabaseAsync("c-vision", "gpt-4o-mini").ConfigureAwait(true);
        ChatDetailViewModel vision = new ChatDetailViewModel(
            "c-vision",
            new CapturingAgentRuntime(),
            visionDatabase.Conversations,
            visionDatabase.Messages,
            imageAttachmentService: attachmentService);
        await vision.LoadAsync(CancellationToken.None).ConfigureAwait(true);

        for (int index = 0; index < 4; index++)
        {
            await vision.AddImageBytesAsync(SmallImage, "image/bmp", CancellationToken.None).ConfigureAwait(true);
        }

        _ = vision.Attachments.Should().HaveCount(ImageAttachmentService.MaxImages);
        _ = vision.StatusMessage.Should().Be("Attachment limit reached");
    }

    [Fact]
    public static async Task CompressionTargetsNextChatDefaultSize()
    {
        ImageAttachmentService attachmentService = CreateAttachmentService();
        byte[] bmp = CreateBmp24(1024, 1024);

        ChatAttachment attachment = await attachmentService.CreateAttachmentAsync(
            bmp,
            "image/bmp",
            targetBytes: 24 * 1024,
            cancellationToken: CancellationToken.None).ConfigureAwait(true);

        _ = attachment.MimeType.Should().Be("image/jpeg");
        _ = attachment.DataUrl.Should().StartWith("data:image/jpeg;base64,");
        _ = attachment.DataUrl.Length.Should().BeLessThanOrEqualTo(24 * 1024);
    }

    [Fact]
    public static async Task HeicMagicBytesRoundTripThroughPngConverter()
    {
        RecordingImageConverter converter = new RecordingImageConverter(SmallImage);
        ImageAttachmentService attachmentService = new ImageAttachmentService(new ImageAttachmentCache(), converter);
        byte[] heic = CreateHeicFixture();

        ChatAttachment attachment = await attachmentService.CreateAttachmentAsync(
            heic,
            "image/heic",
            cancellationToken: CancellationToken.None).ConfigureAwait(true);

        _ = converter.Calls.Should().Be(1);
        _ = attachment.MimeType.Should().Be("image/png");
        _ = attachment.DataUrl.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Async disposal belongs to the test scope.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait", Justification = "Async disposal belongs to the test scope.")]
    public static async Task SubmittedAttachmentsBecomeImageBlocksWithDataUrls()
    {
        CapturingAgentRuntime runtime = new CapturingAgentRuntime();
        await using ArcChatDatabase database = await CreateDatabaseAsync("c-send", "gpt-4o-mini").ConfigureAwait(true);
        ChatDetailViewModel viewModel = new ChatDetailViewModel(
            "c-send",
            runtime,
            database.Conversations,
            database.Messages,
            imageAttachmentService: CreateAttachmentService());
        await viewModel.LoadAsync(CancellationToken.None).ConfigureAwait(true);
        await viewModel.AddImageBytesAsync(SmallImage, "image/bmp", CancellationToken.None).ConfigureAwait(true);
        viewModel.ComposerText = "describe this";

        await viewModel.SubmitAsync(CancellationToken.None).ConfigureAwait(true);

        _ = runtime.Requests.Should().ContainSingle();
        Message userMessage = runtime.Requests[0].Messages.Last(message => message.Role == MessageRole.User);
        _ = userMessage.Content.OfType<TextBlock>().Should().ContainSingle(block => string.Equals(block.Text, "describe this", StringComparison.Ordinal));
        ImageBlock imageBlock = userMessage.Content.OfType<ImageBlock>().Should().ContainSingle().Subject;
        _ = imageBlock.Url.Should().StartWith("data:image/jpeg;base64,");
        _ = viewModel.Attachments.Should().BeEmpty();
    }

    [Fact]
    public static async Task LargeImageConversionKeepsManagedMemoryUnderTwoHundredMegabytes()
    {
        ImageAttachmentService attachmentService = CreateAttachmentService();
        byte[] bmp = CreateBmp24(4096, 4267);
        long before = GC.GetTotalMemory(true);

        ChatAttachment attachment = await attachmentService.CreateAttachmentAsync(
            bmp,
            "image/bmp",
            targetBytes: 64 * 1024,
            cancellationToken: CancellationToken.None).ConfigureAwait(true);

        long after = GC.GetTotalMemory(true);
        _ = attachment.DataUrl.Length.Should().BeLessThanOrEqualTo(64 * 1024);
        _ = (after - before).Should().BeLessThan(200L * 1024L * 1024L);
    }

    private static ImageAttachmentService CreateAttachmentService()
    {
        return new ImageAttachmentService(new ImageAttachmentCache(), new MagickImageConverter());
    }

    private static byte[] CreateBmp24(int width, int height)
    {
        int rowStride = (((width * 24) + 31) / 32) * 4;
        int pixelBytes = rowStride * height;
        byte[] bytes = new byte[54 + pixelBytes];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2, 4), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(10, 4), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(14, 4), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22, 4), height);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(26, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(28, 2), 24);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(34, 4), pixelBytes);
        Array.Fill<byte>(bytes, 0xFF, 54, pixelBytes);
        return bytes;
    }

    private static byte[] CreateHeicFixture()
    {
        byte[] bytes = new byte[32];
        bytes[4] = (byte)'f';
        bytes[5] = (byte)'t';
        bytes[6] = (byte)'y';
        bytes[7] = (byte)'p';
        bytes[8] = (byte)'h';
        bytes[9] = (byte)'e';
        bytes[10] = (byte)'i';
        bytes[11] = (byte)'c';
        return bytes;
    }

    private static async Task<ArcChatDatabase> CreateDatabaseAsync(string conversationId, string model)
    {
        ArcChatDatabase database = new ArcChatDatabase(CreateDatabasePath());
        await database.InitializeAsync(CancellationToken.None).ConfigureAwait(true);
        await database.Conversations.UpsertAsync(CreateConversation(conversationId, model), CancellationToken.None).ConfigureAwait(true);
        return database;
    }

    private static string CreateDatabasePath()
    {
        string directory = Path.Join(Path.GetTempPath(), "ArcChat.Desktop.UiTests");
        Directory.CreateDirectory(directory);
        return Path.Join(directory, Guid.NewGuid().ToString("N") + ".db");
    }

    private static Conversation CreateConversation(string id, string model)
    {
        return new Conversation(
            id,
            "Topic " + id,
            string.Empty,
            ImmutableArray<Message>.Empty,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            new Mask(
                "mask-" + id,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                "1f600",
                "Mask " + id,
                false,
                ImmutableArray<Message>.Empty,
                true,
                ModelConfig.NextChatDefault with { Model = model },
                "en",
                false,
                ImmutableArray<string>.Empty));
    }

    private static async IAsyncEnumerable<ChatEvent> CompletedStream(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(true);
        yield return new MessageCompleted(
            request.ConversationId,
            request.MessageId,
            Message.Text(request.MessageId, MessageRole.Assistant, "ok", "0"));
        yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
    }

    private sealed class CapturingAgentRuntime : IAgentRuntime
    {
        public List<AgentRequest> Requests { get; } = new List<AgentRequest>();

        public IAsyncEnumerable<ChatEvent> StreamAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            this.Requests.Add(request);
            return CompletedStream(request, cancellationToken);
        }
    }

    private sealed class RecordingImageConverter : IImageConverter
    {
        private readonly byte[] output;

        public RecordingImageConverter(byte[] output)
        {
            this.output = output;
        }

        public int Calls { get; private set; }

        public Task<byte[]> HeicToPngAsync(byte[] bytes, CancellationToken cancellationToken = default)
        {
            this.Calls++;
            return Task.FromResult(this.output);
        }
    }
}
