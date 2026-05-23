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

namespace ArcChat.ModelProviders.GenericOpenAi.Tests;

public sealed class GenericOpenAiProviderTests
{
    [Fact]
    public async Task StreamsVllmCompatibleFixtureWithConfiguredEndpointVisionAndTools()
    {
        ProviderConfig config = CreateProviderConfig(
            new Uri("https://unit.test/v1"),
            "keychain://provider/custom-openai/default",
            supportsTools: true,
            supportsVision: true,
            supportsModelList: false,
            "llava-v1.6-vl");
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines("vllm-stream-tools-vision.ndjson")));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(client, config, ResolveApiKey);
        ChatRequest request = CreateRequest("llava-v1.6-vl", CreateVisionMessages(), CreateTools(), maxTokens: 1200);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamReasoning(events, "checking local image");
        ChatProviderContractAssertions.ShouldStreamText(events, "vLLM sees a chart.");
        ChatProviderContractAssertions.ShouldCompleteToolCall(events, "describe_image", "{\"detail\":\"high\"}");
        ChatProviderContractAssertions.ShouldFinish(events, "tool_calls");
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = provider.Id.Value.Should().Be("GenericOpenAI");
        _ = provider.Capabilities.Should().HaveFlag(ChatProviderCapabilities.Tools);
        _ = provider.Capabilities.Should().HaveFlag(ChatProviderCapabilities.Vision);
        _ = handler.RequestUri.Should().Be(new Uri("https://unit.test/v1/chat/completions"));
        _ = handler.Authorization.Should().Be("Bearer test-key");
        _ = root.GetProperty("model").GetString().Should().Be("llava-v1.6-vl");
        _ = root.GetProperty("max_tokens").GetInt32().Should().Be(4000);
        JsonElement userContent = root.GetProperty("messages")[1].GetProperty("content");
        _ = userContent.EnumerateArray().Should().Contain(part => part.GetProperty("type").GetString() == "image_url");
        _ = root.GetProperty("tools").EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task StreamsLmStudioFixtureAgainstBaseUriWithoutVersionSegment()
    {
        ProviderConfig config = CreateProviderConfig(
            new Uri("http://localhost:1234"),
            null,
            supportsTools: false,
            supportsVision: false,
            supportsModelList: false,
            "local-model");
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines("lmstudio-chat-stream.ndjson")));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(client, config);
        ChatRequest request = CreateRequest("local-model", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldStreamText(events, "LM Studio streamed ok.");
        ChatProviderContractAssertions.ShouldFinish(events, "stop");
        _ = handler.RequestUri.Should().Be(new Uri("http://localhost:1234/v1/chat/completions"));
        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        _ = body.RootElement.GetProperty("max_tokens").GetInt32().Should().Be(256);
    }

    [Fact]
    public async Task EndpointCapabilityFlagsDisableVisionPayloadAndTools()
    {
        ProviderConfig config = CreateProviderConfig(
            new Uri("https://unit.test"),
            null,
            supportsTools: false,
            supportsVision: false,
            supportsModelList: false,
            "gpt-4o");
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(["{\"choices\":[{\"delta\":{\"content\":\"ok\"},\"finish_reason\":\"stop\"}]}"]));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(client, config);
        ChatRequest request = CreateRequest("gpt-4o", CreateVisionMessages(), CreateTools(), maxTokens: 512);

        _ = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody);
        JsonElement root = body.RootElement;
        _ = root.GetProperty("messages")[1].GetProperty("content").ValueKind.Should().Be(JsonValueKind.String);
        _ = root.TryGetProperty("tools", out _).Should().BeFalse();
        _ = provider.Capabilities.Should().NotHaveFlag(ChatProviderCapabilities.Tools);
        _ = provider.Capabilities.Should().NotHaveFlag(ChatProviderCapabilities.Vision);
    }

    [Fact]
    public async Task ListsModelsFromOptionalEndpointAndFallsBackToConfiguredModels()
    {
        ProviderConfig config = CreateProviderConfig(
            new Uri("https://unit.test/v1"),
            null,
            supportsTools: true,
            supportsVision: true,
            supportsModelList: true,
            "configured-vl");
        using SequencedHandler handler = new SequencedHandler(
            () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"id\":\"remote-vl\"},{\"id\":\"remote-text\"}]}", Encoding.UTF8, "application/json"),
            },
            () => new HttpResponseMessage(HttpStatusCode.NotFound));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(client, config);

        ImmutableArray<ModelDescriptor> remoteModels = await provider.ListModelsAsync().ConfigureAwait(true);
        ImmutableArray<ModelDescriptor> fallbackModels = await provider.ListModelsAsync().ConfigureAwait(true);

        _ = handler.RequestUris.Should().Equal(
            new Uri("https://unit.test/v1/models"),
            new Uri("https://unit.test/v1/models"));
        _ = remoteModels.Select(model => model.Id).Should().Equal("remote-vl", "remote-text");
        _ = remoteModels[0].Capabilities.Should().Contain(capability => capability is VisionCapability);
        _ = fallbackModels.Should().ContainSingle(model => model.Id == "configured-vl");
    }

    [Fact]
    public async Task HttpErrorsMapToChatErrorEvents()
    {
        using CapturingHandler handler = new CapturingHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limited", Encoding.UTF8, "text/plain"),
        });
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(
            client,
            new GenericOpenAiProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("local-model", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);

        IReadOnlyList<ChatEvent> events = await ChatProviderContractAssertions.CollectAsync(provider, request).ConfigureAwait(true);

        ChatProviderContractAssertions.ShouldContainError(events, "RateLimitedError");
        ChatProviderContractAssertions.ShouldFinish(events, "error");
    }

    [Fact]
    public async Task CancellationAbortsBeforeSending()
    {
        using CapturingHandler handler = new CapturingHandler(() => CreateSseResponse(ReadFixtureLines("lmstudio-chat-stream.ndjson")));
        using HttpClient client = new HttpClient(handler, disposeHandler: false);
        GenericOpenAiProvider provider = new GenericOpenAiProvider(
            client,
            new GenericOpenAiProviderOptions { BaseUri = new Uri("https://unit.test") });
        ChatRequest request = CreateRequest("local-model", CreateSystemAndUserMessages(), ImmutableArray<ArcTool>.Empty, maxTokens: 256);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(true);

        Func<Task> act = async () => await ChatProviderContractAssertions.CollectAsync(provider, request, cancellation.Token).ConfigureAwait(true);

        _ = await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
        _ = handler.SendCount.Should().Be(0);
    }

    private static ProviderConfig CreateProviderConfig(
        Uri baseUri,
        string? apiKeyRef,
        bool supportsTools,
        bool supportsVision,
        bool supportsModelList,
        params string[] modelIds)
    {
        ImmutableArray<ModelDescriptor> models = modelIds.Select((modelId, index) => new ModelDescriptor(
            modelId,
            modelId,
            "custom-openai",
            true,
            -1000 + index,
            ImmutableArray<ProviderCapability>.Empty)).ToImmutableArray();
        return new ProviderConfig(
            "custom-openai",
            "GenericOpenAI",
            baseUri,
            apiKeyRef,
            models,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["supportsTools"] = JsonSerializer.SerializeToElement(supportsTools),
                ["supportsVision"] = JsonSerializer.SerializeToElement(supportsVision),
                ["supportsModelList"] = JsonSerializer.SerializeToElement(supportsModelList),
            }.ToImmutableDictionary(StringComparer.Ordinal));
    }

    private static string? ResolveApiKey(string apiKeyRef)
    {
        return string.Equals(apiKeyRef, "keychain://provider/custom-openai/default", StringComparison.Ordinal)
            ? "test-key"
            : null;
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
            ModelConfig.NextChatDefault with { Model = model, ProviderName = "GenericOpenAI", MaxTokens = maxTokens },
            tools,
            ProviderExtra.ForStream("c1", "a1"));
    }

    private static string[] ReadFixtureLines(string name)
    {
        if (Path.IsPathRooted(name))
        {
            throw new ArgumentException("Fixture name must be relative.", nameof(name));
        }

        return File.ReadAllLines(Path.Join(AppContext.BaseDirectory, "Resources", name));
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

    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> responseFactories;

        public SequencedHandler(params Func<HttpResponseMessage>[] responseFactories)
        {
            this.responseFactories = new Queue<Func<HttpResponseMessage>>(responseFactories);
        }

        public List<Uri?> RequestUris { get; } = new List<Uri?>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.RequestUris.Add(request.RequestUri);
            return Task.FromResult(this.responseFactories.Dequeue()());
        }
    }
}
