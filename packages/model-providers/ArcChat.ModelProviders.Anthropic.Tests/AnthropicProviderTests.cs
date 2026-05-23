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

namespace ArcChat.ModelProviders.Anthropic.Tests;

public sealed class AnthropicProviderTests
{
    private static readonly string[] SimpleStopStream =
    [
        "{\"type\":\"message_start\",\"message\":{\"id\":\"msg_unit\",\"type\":\"message\",\"role\":\"assistant\",\"content\":[],\"model\":\"claude-3-5-sonnet-latest\",\"stop_reason\":null}}",
        "{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}",
        "{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"ok\"}}",
        "{\"type\":\"content_block_stop\",\"index\":0}",
        "{\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"end_turn\",\"stop_sequence\":null}}",
        "{\"type\":\"message_stop\"}",
    ];

    [Fact]
    public async Task StreamsAnthropicFixtureWithVisionToolsAndThinking()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        AnthropicProvider provider = new AnthropicProvider(
            client,
            new AnthropicProviderOptions { BaseUri = new Uri("https://unit.test"), ApiKey = "test-key", ApiVersion = "2023-06-01" });
        ChatRequest request = CreateRequest("claude-sonnet-4-20250514", CreateVisionMessages(), CreateTools(), maxTokens: 1200);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamReasoning(events, "checking image");
        ChatProviderContractAssertions.ShouldStreamText(events, "The image shows a chart.");
        ChatProviderContractAssertions.ShouldCompleteToolCall(events, "describe_image", "{\"detail\":\"high\"}");
        ChatProviderContractAssertions.ShouldFinish(events, "tool_use");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = handler.RequestUri.Should().Be(new Uri("https://unit.test/v1/messages"));
        _ = handler.ApiKey.Should().Be("test-key");
        _ = handler.ApiVersion.Should().Be("2023-06-01");
        _ = root.GetProperty("model").GetString().Should().Be("claude-sonnet-4-20250514");
        _ = root.GetProperty("max_tokens").GetInt32().Should().Be(1200);
        _ = root.GetProperty("system").EnumerateArray().Should().ContainSingle();
        _ = root.GetProperty("messages").EnumerateArray().Should().NotContain(message => message.GetProperty("role").GetString() == "system");
        JsonElement userContent = root.GetProperty("messages")[0].GetProperty("content");
        _ = userContent.EnumerateArray().Should().Contain(part => part.GetProperty("type").GetString() == "image");
        _ = root.GetProperty("tools").EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task AddsCacheControlToSystemToolsAndMessages()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(SimpleStopStream));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        AnthropicProvider provider = new AnthropicProvider(
            client,
            new AnthropicProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("claude-3-5-sonnet-latest", CreateSystemAndUserMessages(), CreateTools(), maxTokens: 0);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamText(events, "ok");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = root.GetProperty("max_tokens").GetInt32().Should().Be(4096);
        _ = root.GetProperty("system")[0].TryGetProperty("cache_control", out _).Should().BeTrue();
        _ = root.GetProperty("tools")[0].TryGetProperty("cache_control", out _).Should().BeTrue();
        _ = root.GetProperty("messages")[0].GetProperty("content")[0].TryGetProperty("cache_control", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SerializesToolUseAndToolResultHistory()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(SimpleStopStream));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        AnthropicProvider provider = new AnthropicProvider(
            client,
            new AnthropicProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("claude-3-5-sonnet-latest", CreateToolRoundTripMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        _ = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement messages = body.RootElement.GetProperty("messages");
        _ = messages[1].GetProperty("content").EnumerateArray().Should().Contain(block => block.GetProperty("type").GetString() == "tool_use");
        _ = messages[2].GetProperty("content").EnumerateArray().Should().Contain(block => block.GetProperty("type").GetString() == "tool_result");
    }

    [Fact]
    public async Task HttpErrorsMapToChatErrorEvents()
    {
        using CapturingHandler handler = new CapturingHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limited", Encoding.UTF8, "text/plain"),
        });
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        AnthropicProvider provider = new AnthropicProvider(
            client,
            new AnthropicProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("claude-3-5-sonnet-latest", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldContainError(events, "RateLimitedError");
        ChatProviderContractAssertions.ShouldFinish(events, "error");
    }

    [Fact]
    public async Task CancellationAbortsBeforeSending()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        AnthropicProvider provider = new AnthropicProvider(
            client,
            new AnthropicProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("claude-3-5-sonnet-latest", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);
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
            _ = builder.Append("event: ").Append(GetEventName(line)).Append('\n');
            _ = builder.Append("data: ").Append(line).Append("\n\n");
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream"),
        };
    }

    private static string GetEventName(string line)
    {
        using JsonDocument document = JsonDocument.Parse(line);
        return document.RootElement.GetProperty("type").GetString() ?? "message";
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
                ImmutableArray.Create<ContentBlock>(new ToolCallBlock("toolu_1", "weather", arguments.RootElement.Clone())),
                "0",
                Tools: ImmutableArray<ChatMessageTool>.Empty),
            new Message(
                "u2",
                MessageRole.User,
                ImmutableArray.Create<ContentBlock>(new ToolResultBlock("toolu_1", "weather", "sunny")),
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
            ModelConfig.NextChatDefault with { Model = model, ProviderName = "Anthropic", MaxTokens = maxTokens },
            tools,
            ProviderExtra.ForStream("c1", "a1"));
    }

    private static string[] ReadFixtureLines()
    {
        return File.ReadAllLines(Path.Join(AppContext.BaseDirectory, "Resources", "anthropic-messages-tools-vision.ndjson"));
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

        public string? ApiVersion { get; private set; }

        public int SendCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.SendCount++;
            this.RequestUri = request.RequestUri;
            this.ApiKey = request.Headers.TryGetValues("x-api-key", out IEnumerable<string>? apiKeys) ? apiKeys.SingleOrDefault() : null;
            this.ApiVersion = request.Headers.TryGetValues("anthropic-version", out IEnumerable<string>? versions) ? versions.SingleOrDefault() : null;
            this.RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return this.responseFactory();
        }
    }
}
