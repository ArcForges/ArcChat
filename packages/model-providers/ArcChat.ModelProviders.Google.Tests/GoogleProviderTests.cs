// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using ArcChat.ModelProviders.Core;
using ArcChat.ModelProviders.Core.ContractTestKit;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.ModelProviders.Google.Tests;

public sealed class GoogleProviderTests
{
    private static readonly string[] SimpleStopStream =
    [
        "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":\"ok\"}]},\"finishReason\":\"STOP\"}]}",
    ];

    [Fact]
    public async Task StreamsGoogleFixtureWithVisionToolsAndSafetySettings()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GoogleProvider provider = new GoogleProvider(
            client,
            new GoogleProviderOptions
            {
                BaseUri = new Uri("https://unit.test"),
                ApiKey = "test-key",
                SafetyThreshold = GoogleSafetySettingsThreshold.BlockNone,
            });
        ChatRequest request = CreateRequest("gemini-2.5-pro", CreateVisionMessages(), CreateTools(), maxTokens: 1200);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamText(events, "The image shows a chart.");
        ChatProviderContractAssertions.ShouldCompleteToolCall(events, "describe_image", "{\"detail\":\"high\"}");
        ChatProviderContractAssertions.ShouldFinish(events, "STOP");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = handler.RequestUri.Should().Be(new Uri("https://unit.test/v1beta/models/gemini-2.5-pro:streamGenerateContent?alt=sse"));
        _ = handler.ApiKey.Should().Be("test-key");
        _ = root.GetProperty("generationConfig").GetProperty("maxOutputTokens").GetInt32().Should().Be(1200);
        _ = root.GetProperty("systemInstruction").GetProperty("parts").EnumerateArray().Should().ContainSingle();
        JsonElement userParts = root.GetProperty("contents")[0].GetProperty("parts");
        _ = userParts.EnumerateArray().Should().Contain(part => HasProperty(part, "inlineData"));
        _ = root.GetProperty("tools")[0].GetProperty("functionDeclarations").EnumerateArray().Should().ContainSingle();
        _ = root.GetProperty("safetySettings").EnumerateArray().Should().OnlyContain(setting => setting.GetProperty("threshold").GetString() == "BLOCK_NONE");
    }

    [Fact]
    public async Task SerializesToolCallAndFunctionResponseHistory()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(SimpleStopStream));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GoogleProvider provider = new GoogleProvider(
            client,
            new GoogleProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gemini-2.0-flash", CreateToolRoundTripMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        _ = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement contents = body.RootElement.GetProperty("contents");
        _ = contents[1].GetProperty("role").GetString().Should().Be("model");
        _ = contents[1].GetProperty("parts").EnumerateArray().Should().Contain(part => HasProperty(part, "functionCall"));
        _ = contents[2].GetProperty("role").GetString().Should().Be("user");
        _ = contents[2].GetProperty("parts").EnumerateArray().Should().Contain(part => HasProperty(part, "functionResponse"));
    }

    [Fact]
    public async Task PrependsUserPreambleWhenFirstContentIsModel()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(SimpleStopStream));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GoogleProvider provider = new GoogleProvider(
            client,
            new GoogleProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest(
            "gemini-2.0-flash",
            ImmutableArray.Create(Message.Text("a1", MessageRole.Assistant, "Already started.", "0")),
            ImmutableArray<ArcTool>.Empty,
            maxTokens: 256);

        _ = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement contents = body.RootElement.GetProperty("contents");
        _ = contents[0].GetProperty("role").GetString().Should().Be("user");
        _ = contents[0].GetProperty("parts")[0].GetProperty("text").GetString().Should().Be(";");
        _ = contents[1].GetProperty("role").GetString().Should().Be("model");
    }

    [Fact]
    public async Task HttpErrorsMapToChatErrorEvents()
    {
        using CapturingHandler handler = new CapturingHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limited", Encoding.UTF8, "text/plain"),
        });
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GoogleProvider provider = new GoogleProvider(
            client,
            new GoogleProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gemini-2.0-flash", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldContainError(events, "RateLimitedError");
        ChatProviderContractAssertions.ShouldFinish(events, "error");
    }

    [Fact]
    public async Task CancellationAbortsBeforeSending()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GoogleProvider provider = new GoogleProvider(
            client,
            new GoogleProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gemini-2.0-flash", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(true);

        Func<Task> act = async () => await ChatProviderContractAssertions.CollectAsync(provider, request, cancellation.Token).ConfigureAwait(true);

        _ = await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
        _ = handler.SendCount.Should().Be(0);
    }

    private static HttpResponseMessage CreateSseResponse(IEnumerable<string> lines)
    {
        StringBuilder builder = new StringBuilder();
        foreach (string line in lines)
        {
            _ = builder.Append("data: ").Append(line).Append("\n\n");
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream"),
        };
    }

    private static ImmutableArray<Message> CreateSystemAndUserMessages()
    {
        return ImmutableArray.Create(
            Message.Text("s1", MessageRole.System, "Be concise.", "0"),
            Message.Text("u1", MessageRole.User, "Say ok.", "0"));
    }

    private static ImmutableArray<Message> CreateVisionMessages()
    {
        return ImmutableArray.Create(
            Message.Text("s1", MessageRole.System, "Use tools when helpful.", "0"),
            new Message(
                "u1",
                MessageRole.User,
                ImmutableArray.Create<ContentBlock>(
                    new TextBlock("Describe this image."),
                    new ImageBlock("data:image/png;base64,abc", "high")),
                "0",
                Tools: ImmutableArray<ChatMessageTool>.Empty));
    }

    private static ImmutableArray<Message> CreateToolRoundTripMessages()
    {
        using JsonDocument arguments = JsonDocument.Parse("{\"city\":\"Paris\"}");
        return ImmutableArray.Create(
            Message.Text("u1", MessageRole.User, "Weather?", "0"),
            new Message(
                "a1",
                MessageRole.Assistant,
                ImmutableArray.Create<ContentBlock>(new ToolCallBlock("call_1", "weather", arguments.RootElement.Clone())),
                "0",
                Tools: ImmutableArray<ChatMessageTool>.Empty),
            new Message(
                "u2",
                MessageRole.User,
                ImmutableArray.Create<ContentBlock>(new ToolResultBlock("call_1", "weather", "sunny")),
                "0",
                Tools: ImmutableArray<ChatMessageTool>.Empty));
    }

    private static ImmutableArray<ArcTool> CreateTools()
    {
        using JsonDocument schema = JsonDocument.Parse(
            """
            {
              "type": "object",
              "properties": {
                "detail": { "type": "string" }
              }
            }
            """);
        return ImmutableArray.Create(new ArcTool(
            "describe_image",
            "describe_image",
            "Describe an image.",
            schema.RootElement.Clone(),
            JsonSerializer.SerializeToElement(new { type = "object" }),
            ToolPermissionKind.Read));
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private static ChatRequest CreateRequest(
        string model,
        ImmutableArray<Message> messages,
        ImmutableArray<ArcTool> tools,
        int maxTokens)
    {
        return new ChatRequest(
            messages,
            ModelConfig.NextChatDefault with { Model = model, ProviderName = "Google", MaxTokens = maxTokens },
            tools,
            ProviderExtra.ForStream("c1", "a1"));
    }

    private static string[] ReadFixtureLines()
    {
        return File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "google-stream-tools-vision.ndjson"));
    }

    private static bool HasProperty(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out _);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> responseFactory;

        public CapturingHandler(Func<HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public string RequestBody { get; private set; } = string.Empty;

        public Uri? RequestUri { get; private set; }

        public string? ApiKey { get; private set; }

        public int SendCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.SendCount++;
            this.RequestUri = request.RequestUri;
            this.ApiKey = request.Headers.TryGetValues("x-goog-api-key", out IEnumerable<string>? apiKeys) ? apiKeys.SingleOrDefault() : null;
            this.RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return this.responseFactory();
        }
    }
}
