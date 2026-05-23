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

namespace ArcChat.ModelProviders.OpenAi.Tests;

public sealed class OpenAiProviderTests
{
    private static readonly string[] SimpleStopStream =
    [
        "{\"choices\":[{\"delta\":{\"content\":\"ok\"},\"finish_reason\":\"stop\"}]}",
    ];

    [Fact]
    public async Task StreamsOpenAiFixtureWithVisionToolsAndReasoning()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        OpenAiProvider provider = new OpenAiProvider(
            client,
            new OpenAiProviderOptions { BaseUri = new Uri("https://unit.test"), ApiKey = "test-key" });
        ChatRequest request = CreateRequest("gpt-4o-mini", CreateVisionMessages(), CreateTools(), maxTokens: 1200);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamReasoning(events, "checking image");
        ChatProviderContractAssertions.ShouldStreamText(events, "The image shows a chart.");
        ChatProviderContractAssertions.ShouldCompleteToolCall(events, "describe_image", "{\"detail\":\"high\"}");
        ChatProviderContractAssertions.ShouldFinish(events, "tool_calls");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = handler.RequestUri.Should().Be(new Uri("https://unit.test/v1/chat/completions"));
        _ = handler.Authorization.Should().Be("Bearer test-key");
        _ = root.GetProperty("model").GetString().Should().Be("gpt-4o-mini");
        _ = root.GetProperty("max_tokens").GetInt32().Should().Be(4000);
        _ = root.TryGetProperty("max_completion_tokens", out _).Should().BeFalse();
        JsonElement userContent = root.GetProperty("messages")[1].GetProperty("content");
        _ = userContent.EnumerateArray().Should().Contain(part => part.GetProperty("type").GetString() == "image_url");
        _ = root.GetProperty("tools").EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task ReasoningModelsUseDeveloperRoleAndMaxCompletionTokens()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(SimpleStopStream));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        OpenAiProvider provider = new OpenAiProvider(
            client,
            new OpenAiProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gpt-5", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 512);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamText(events, "ok");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = root.GetProperty("messages")[0].GetProperty("role").GetString().Should().Be("developer");
        _ = root.GetProperty("max_completion_tokens").GetInt32().Should().Be(512);
        _ = root.TryGetProperty("max_tokens", out _).Should().BeFalse();
        _ = root.GetProperty("temperature").GetDouble().Should().Be(1);
        _ = root.GetProperty("top_p").GetDouble().Should().Be(1);
    }

    [Fact]
    public async Task HttpErrorsMapToChatErrorEvents()
    {
        using CapturingHandler handler = new CapturingHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limited", Encoding.UTF8, "text/plain"),
        });
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        OpenAiProvider provider = new OpenAiProvider(
            client,
            new OpenAiProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gpt-4o-mini", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldContainError(events, "RateLimitedError");
        ChatProviderContractAssertions.ShouldFinish(events, "error");
    }

    [Fact]
    public async Task CancellationAbortsBeforeSending()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines()));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        OpenAiProvider provider = new OpenAiProvider(
            client,
            new OpenAiProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("gpt-4o-mini", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);
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

        _ = builder.Append("data: [DONE]\n\n");
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
            ModelConfig.NextChatDefault with { Model = model, ProviderName = "OpenAI", MaxTokens = maxTokens },
            tools,
            ProviderExtra.ForStream("c1", "a1"));
    }

    private static string[] ReadFixtureLines()
    {
        return File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "openai-stream-tools-vision.ndjson"));
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

        public string? Authorization { get; private set; }

        public int SendCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.SendCount++;
            this.RequestUri = request.RequestUri;
            this.Authorization = request.Headers.Authorization?.ToString();
            this.RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return this.responseFactory();
        }
    }
}
