// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ArcChat.ModelProviders.Core;
using ArcChat.Net.Errors;
using ArcChat.Net.Sse;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.GenericOpenAi;

/// <summary>
/// Generic OpenAI-compatible chat completions provider for user-configured endpoints.
/// </summary>
public sealed class GenericOpenAiProvider : IChatProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly ServerSentEventReader SseReader = new ServerSentEventReader();

    private readonly HttpClient httpClient;
    private readonly GenericOpenAiProviderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericOpenAiProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    public GenericOpenAiProvider(HttpClient httpClient)
        : this(httpClient, new GenericOpenAiProviderOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericOpenAiProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="options">Generic OpenAI-compatible provider options.</param>
    public GenericOpenAiProvider(HttpClient httpClient, GenericOpenAiProviderOptions options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericOpenAiProvider"/> class from persisted provider settings.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="providerConfig">Persisted provider configuration.</param>
    public GenericOpenAiProvider(
        HttpClient httpClient,
        ProviderConfig providerConfig)
        : this(httpClient, GenericOpenAiProviderOptions.FromProviderConfig(providerConfig))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericOpenAiProvider"/> class from persisted provider settings.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="providerConfig">Persisted provider configuration.</param>
    /// <param name="apiKeyResolver">API key reference resolver.</param>
    public GenericOpenAiProvider(
        HttpClient httpClient,
        ProviderConfig providerConfig,
        Func<string, string?> apiKeyResolver)
        : this(httpClient, GenericOpenAiProviderOptions.FromProviderConfig(providerConfig, apiKeyResolver))
    {
    }

    /// <inheritdoc />
    public ProviderId Id => new ProviderId(this.options.ProviderName);

    /// <inheritdoc />
    public ChatProviderCapabilities Capabilities
    {
        get
        {
            ChatProviderCapabilities capabilities = ChatProviderCapabilities.Streaming;
            if (this.options.SupportsTools)
            {
                capabilities |= ChatProviderCapabilities.Tools;
            }

            if (this.options.SupportsVision)
            {
                capabilities |= ChatProviderCapabilities.Vision;
            }

            if (this.options.Models.Any(model => model.Capabilities.OfType<ReasoningCapability>().Any()))
            {
                capabilities |= ChatProviderCapabilities.Reasoning;
            }

            return capabilities;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpRequestMessage httpRequest = this.CreateChatRequest(request);
        using HttpResponseMessage response = await this.httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string? errorText = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            NetError error = NetErrorMapper.FromResponse(response, string.IsNullOrWhiteSpace(errorText) ? null : errorText);
            yield return new ChatError(request.Extra.ConversationId, request.Extra.MessageId, error.GetType().Name, error.Message);
            yield return new ChatFinished(request.Extra.ConversationId, request.Extra.MessageId, "error");
            yield break;
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        GenericOpenAiStreamState state = new GenericOpenAiStreamState(request);
        await foreach (SseEvent sseEvent in SseReader.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            foreach (ChatEvent chatEvent in state.Apply(sseEvent.Data))
            {
                yield return chatEvent;
            }

            if (state.IsComplete)
            {
                yield break;
            }
        }

        foreach (ChatEvent chatEvent in state.Complete(null))
        {
            yield return chatEvent;
        }
    }

    /// <inheritdoc />
    public async Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        if (!this.options.SupportsModelList)
        {
            return this.options.Models;
        }

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, this.CreateModelsUri());
        this.AddAuthorization(httpRequest);
        using HttpResponseMessage response = await this.httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return this.options.Models;
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("data", out JsonElement data) || data.ValueKind is not JsonValueKind.Array)
            {
                return this.options.Models;
            }

            IEnumerable<string> modelIds = data.EnumerateArray()
                .Select(ReadModelId)
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Select(static id => id!);
            ImmutableArray<ModelDescriptor> remoteModels = GenericOpenAiModelCatalog.FromModelIds(
                modelIds,
                this.options.ProviderConfigId,
                this.options.SupportsTools,
                this.options.SupportsVision);
            return remoteModels.IsDefaultOrEmpty ? this.options.Models : remoteModels;
        }
        catch (JsonException)
        {
            return this.options.Models;
        }
    }

    private static string? ReadModelId(JsonElement modelElement)
    {
        return modelElement.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind is JsonValueKind.String
            ? idElement.GetString()
            : null;
    }

    private static GenericOpenAiPayload.Message CreateMessage(Message message, bool visionModel)
    {
        string role = message.Role switch
        {
            MessageRole.System => "system",
            MessageRole.User => "user",
            MessageRole.Assistant => "assistant",
            _ => "user",
        };

        JsonElement content = visionModel
            ? JsonSerializer.SerializeToElement(CreateContentParts(message), JsonOptions)
            : JsonSerializer.SerializeToElement(GetTextContent(message), JsonOptions);

        return new GenericOpenAiPayload.Message(role, content);
    }

    private static ImmutableArray<GenericOpenAiPayload.ContentPart> CreateContentParts(Message message)
    {
        ImmutableArray<GenericOpenAiPayload.ContentPart>.Builder parts =
            ImmutableArray.CreateBuilder<GenericOpenAiPayload.ContentPart>();
        string text = GetTextContent(message);
        if (!string.IsNullOrEmpty(text))
        {
            parts.Add(new GenericOpenAiPayload.ContentPart("text", text: text));
        }

        foreach (ImageBlock image in message.Content.OfType<ImageBlock>())
        {
            parts.Add(
                new GenericOpenAiPayload.ContentPart(
                    "image_url",
                    imageUrl: new GenericOpenAiPayload.ImageUrl(image.Url, image.Detail)));
        }

        return parts.ToImmutable();
    }

    private static GenericOpenAiPayload.Tool CreateTool(ArcTool tool)
    {
        return new GenericOpenAiPayload.Tool(
            "function",
            new GenericOpenAiPayload.FunctionTool(tool.Name, tool.Description, tool.InputSchema));
    }

    private static string GetTextContent(Message message)
    {
        return string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    private static Uri AppendOpenAiPath(Uri baseUri, string pathFromV1)
    {
        string prefix = baseUri.ToString().TrimEnd('/');
        string path = prefix.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? pathFromV1.TrimStart('/')
            : "v1/" + pathFromV1.TrimStart('/');
        return new Uri($"{prefix}/{path}", UriKind.Absolute);
    }

    private GenericOpenAiPayload.ChatRequest CreatePayload(ChatRequest request)
    {
        string modelId = request.Config.Model;
        bool reasoningModel = GenericOpenAiModelCatalog.IsReasoningModel(modelId);
        bool visionModel = this.options.SupportsVision && GenericOpenAiModelCatalog.IsVisionModel(modelId);
        ImmutableArray<GenericOpenAiPayload.Message>.Builder messages =
            ImmutableArray.CreateBuilder<GenericOpenAiPayload.Message>();
        foreach (Message message in request.History)
        {
            messages.Add(CreateMessage(message, visionModel));
        }

        int? maxTokens = reasoningModel ? null : request.Config.MaxTokens;
        int? maxCompletionTokens = reasoningModel ? request.Config.MaxTokens : null;
        if (visionModel && maxTokens is int configuredMaxTokens)
        {
            maxTokens = Math.Max(configuredMaxTokens, 4000);
        }

        ImmutableArray<GenericOpenAiPayload.Tool>? tools = this.options.SupportsTools && !request.Tools.IsDefaultOrEmpty
            ? request.Tools.Select(CreateTool).ToImmutableArray()
            : null;

        return new GenericOpenAiPayload.ChatRequest(
            modelId,
            messages.ToImmutable(),
            request.Config.Stream,
            reasoningModel ? 1 : request.Config.Temperature,
            reasoningModel ? 0 : request.Config.PresencePenalty,
            reasoningModel ? 0 : request.Config.FrequencyPenalty,
            reasoningModel ? 1 : request.Config.TopP,
            maxTokens,
            maxCompletionTokens,
            tools);
    }

    private HttpRequestMessage CreateChatRequest(ChatRequest request)
    {
        GenericOpenAiPayload.ChatRequest payload = this.CreatePayload(request);
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.CreateChatUri())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        this.AddAuthorization(httpRequest);
        return httpRequest;
    }

    private void AddAuthorization(HttpRequestMessage httpRequest)
    {
        string? apiKey = this.options.ResolveApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private Uri CreateChatUri()
    {
        return AppendOpenAiPath(this.options.BaseUri, "chat/completions");
    }

    private Uri CreateModelsUri()
    {
        return AppendOpenAiPath(this.options.BaseUri, "models");
    }

    private sealed class GenericOpenAiStreamState
    {
        private readonly ChatRequest request;
        private readonly StringBuilder content = new StringBuilder();
        private readonly Dictionary<int, GenericOpenAiPayload.ToolCallAccumulator> tools =
            new Dictionary<int, GenericOpenAiPayload.ToolCallAccumulator>();

        private readonly HashSet<int> startedToolIndexes = new HashSet<int>();

        public GenericOpenAiStreamState(ChatRequest request)
        {
            this.request = request;
        }

        public bool IsComplete { get; private set; }

        public IEnumerable<ChatEvent> Apply(string data)
        {
            if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
            {
                foreach (ChatEvent chatEvent in this.Complete(null))
                {
                    yield return chatEvent;
                }

                yield break;
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                yield break;
            }

            using JsonDocument document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
            {
                yield break;
            }

            JsonElement choice = choices[0];
            if (choice.TryGetProperty("delta", out JsonElement delta))
            {
                foreach (ChatEvent chatEvent in this.ApplyDelta(delta))
                {
                    yield return chatEvent;
                }
            }

            string? finishReason = null;
            if (choice.TryGetProperty("finish_reason", out JsonElement finishReasonElement)
                && finishReasonElement.ValueKind is JsonValueKind.String)
            {
                finishReason = finishReasonElement.GetString();
            }

            if (!string.IsNullOrEmpty(finishReason))
            {
                foreach (ChatEvent chatEvent in this.Complete(finishReason))
                {
                    yield return chatEvent;
                }
            }
        }

        public IEnumerable<ChatEvent> Complete(string? finishReason)
        {
            if (this.IsComplete)
            {
                yield break;
            }

            foreach (GenericOpenAiPayload.ToolCallAccumulator tool in this.tools.Values.OrderBy(tool => tool.Index))
            {
                yield return new ToolCallCompleted(
                    this.request.Extra.ConversationId,
                    this.request.Extra.MessageId,
                    new ChatMessageTool(tool.Id, tool.Name, tool.Arguments, tool.Index, tool.Type),
                    CreateToolResult(tool.Arguments));
            }

            Message message = new Message(
                this.request.Extra.MessageId,
                MessageRole.Assistant,
                ImmutableArray.Create<ContentBlock>(new TextBlock(this.content.ToString())),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                Model: this.request.Config.Model,
                Tools: this.tools.Values
                    .OrderBy(tool => tool.Index)
                    .Select(tool => new ChatMessageTool(tool.Id, tool.Name, tool.Arguments, tool.Index, tool.Type))
                    .ToImmutableArray());

            yield return new MessageCompleted(this.request.Extra.ConversationId, this.request.Extra.MessageId, message);
            yield return new ChatFinished(this.request.Extra.ConversationId, this.request.Extra.MessageId, finishReason);
            this.IsComplete = true;
        }

        private static JsonElement CreateToolResult(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return JsonSerializer.SerializeToElement(string.Empty, JsonOptions);
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(arguments);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return JsonSerializer.SerializeToElement(arguments, JsonOptions);
            }
        }

        private IEnumerable<ChatEvent> ApplyDelta(JsonElement delta)
        {
            if (delta.TryGetProperty("reasoning_content", out JsonElement reasoningElement)
                && reasoningElement.ValueKind is JsonValueKind.String)
            {
                string? reasoning = reasoningElement.GetString();
                if (!string.IsNullOrEmpty(reasoning))
                {
                    yield return new ReasoningDelta(this.request.Extra.ConversationId, this.request.Extra.MessageId, reasoning);
                }
            }

            if (delta.TryGetProperty("content", out JsonElement contentElement) && contentElement.ValueKind is JsonValueKind.String)
            {
                string? text = contentElement.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    _ = this.content.Append(text);
                    yield return new MessageDelta(this.request.Extra.ConversationId, this.request.Extra.MessageId, text);
                }
            }

            if (delta.TryGetProperty("tool_calls", out JsonElement toolCallsElement)
                && toolCallsElement.ValueKind is JsonValueKind.Array)
            {
                foreach (JsonElement toolCall in toolCallsElement.EnumerateArray())
                {
                    foreach (ChatEvent chatEvent in this.ApplyToolCall(toolCall))
                    {
                        yield return chatEvent;
                    }
                }
            }
        }

        private IEnumerable<ChatEvent> ApplyToolCall(JsonElement toolCall)
        {
            int index = 0;
            if (toolCall.TryGetProperty("index", out JsonElement indexElement) && indexElement.TryGetInt32(out int parsedIndex))
            {
                index = parsedIndex;
            }

            if (!this.tools.TryGetValue(index, out GenericOpenAiPayload.ToolCallAccumulator? accumulator))
            {
                accumulator = new GenericOpenAiPayload.ToolCallAccumulator(index);
                this.tools.Add(index, accumulator);
            }

            accumulator.Apply(toolCall);
            if (accumulator.IsStarted && this.startedToolIndexes.Add(index))
            {
                yield return new ToolCallStarted(
                    this.request.Extra.ConversationId,
                    this.request.Extra.MessageId,
                    new ChatMessageTool(accumulator.Id, accumulator.Name, accumulator.Arguments, accumulator.Index, accumulator.Type));
            }
        }
    }
}
