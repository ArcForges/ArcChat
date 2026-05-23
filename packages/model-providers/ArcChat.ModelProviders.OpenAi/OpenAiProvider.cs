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

namespace ArcChat.ModelProviders.OpenAi;

/// <summary>
/// OpenAI chat completions provider.
/// </summary>
public sealed class OpenAiProvider : IChatProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly ServerSentEventReader SseReader = new ServerSentEventReader();

    private readonly HttpClient httpClient;
    private readonly OpenAiProviderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="options">Optional OpenAI provider options.</param>
    public OpenAiProvider(HttpClient httpClient, OpenAiProviderOptions? options = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? new OpenAiProviderOptions();
    }

    /// <inheritdoc />
    public ProviderId Id => new ProviderId("OpenAI");

    /// <inheritdoc />
    public ChatProviderCapabilities Capabilities =>
        ChatProviderCapabilities.Streaming
        | ChatProviderCapabilities.Tools
        | ChatProviderCapabilities.Vision
        | ChatProviderCapabilities.Reasoning;

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpRequestMessage httpRequest = this.CreateHttpRequest(request);
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
        OpenAiStreamState state = new OpenAiStreamState(request);
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
    public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.options.Models);
    }

    internal static bool IsReasoningModel(string modelId)
    {
        return modelId.StartsWith("o1", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o3", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o4", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVisionModel(string modelId)
    {
        return modelId.Contains("vision", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gpt-4o", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gpt-4.1", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gpt-5", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("o3", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("o4-mini", StringComparison.OrdinalIgnoreCase)
            || (modelId.Contains("gpt-4-turbo", StringComparison.OrdinalIgnoreCase)
                && !modelId.Contains("preview", StringComparison.OrdinalIgnoreCase));
    }

    private static OpenAiPayload.Message CreateMessage(Message message, bool useDeveloperRole, bool visionModel)
    {
        string role = message.Role switch
        {
            MessageRole.System when useDeveloperRole => "developer",
            MessageRole.System => "system",
            MessageRole.User => "user",
            MessageRole.Assistant => "assistant",
            _ => "user",
        };

        JsonElement content = visionModel
            ? JsonSerializer.SerializeToElement(CreateContentParts(message), JsonOptions)
            : JsonSerializer.SerializeToElement(GetTextContent(message), JsonOptions);

        return new OpenAiPayload.Message(role, content);
    }

    private static ImmutableArray<OpenAiPayload.ContentPart> CreateContentParts(Message message)
    {
        ImmutableArray<OpenAiPayload.ContentPart>.Builder parts = ImmutableArray.CreateBuilder<OpenAiPayload.ContentPart>();
        string text = GetTextContent(message);
        if (!string.IsNullOrEmpty(text))
        {
            parts.Add(new OpenAiPayload.ContentPart("text", text: text));
        }

        foreach (ImageBlock image in message.Content.OfType<ImageBlock>())
        {
            parts.Add(new OpenAiPayload.ContentPart("image_url", imageUrl: new OpenAiPayload.ImageUrl(image.Url, image.Detail)));
        }

        return parts.ToImmutable();
    }

    private static OpenAiPayload.ChatRequest CreatePayload(ChatRequest request)
    {
        string modelId = request.Config.Model;
        bool reasoningModel = IsReasoningModel(modelId);
        bool visionModel = IsVisionModel(modelId);
        ImmutableArray<OpenAiPayload.Message>.Builder messages = ImmutableArray.CreateBuilder<OpenAiPayload.Message>();
        foreach (Message message in request.History)
        {
            messages.Add(CreateMessage(message, reasoningModel, visionModel));
        }

        int? maxTokens = null;
        int? maxCompletionTokens = null;
        if (reasoningModel)
        {
            maxCompletionTokens = request.Config.MaxTokens;
        }
        else if (visionModel)
        {
            maxTokens = Math.Max(request.Config.MaxTokens, 4000);
        }

        ImmutableArray<OpenAiPayload.Tool>? tools = request.Tools.IsDefaultOrEmpty
            ? null
            : request.Tools.Select(CreateTool).ToImmutableArray();

        return new OpenAiPayload.ChatRequest(
            modelId,
            messages.ToImmutable(),
            true,
            reasoningModel ? 1 : request.Config.Temperature,
            reasoningModel ? 0 : request.Config.PresencePenalty,
            reasoningModel ? 0 : request.Config.FrequencyPenalty,
            reasoningModel ? 1 : request.Config.TopP,
            maxTokens,
            maxCompletionTokens,
            tools);
    }

    private static OpenAiPayload.Tool CreateTool(ArcTool tool)
    {
        return new OpenAiPayload.Tool(
            "function",
            new OpenAiPayload.FunctionTool(tool.Name, tool.Description, tool.InputSchema));
    }

    private static string GetTextContent(Message message)
    {
        return string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    private HttpRequestMessage CreateHttpRequest(ChatRequest request)
    {
        OpenAiPayload.ChatRequest payload = CreatePayload(request);
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.CreateChatUri())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.options.ApiKey);
        }

        return httpRequest;
    }

    private Uri CreateChatUri()
    {
        string baseUri = this.options.BaseUri.ToString().TrimEnd('/');
        return new Uri($"{baseUri}/v1/chat/completions", UriKind.Absolute);
    }

    private sealed class OpenAiStreamState
    {
        private readonly ChatRequest request;
        private readonly StringBuilder content = new StringBuilder();
        private readonly Dictionary<int, OpenAiPayload.ToolCallAccumulator> tools = new Dictionary<int, OpenAiPayload.ToolCallAccumulator>();
        private readonly HashSet<int> startedToolIndexes = new HashSet<int>();

        public OpenAiStreamState(ChatRequest request)
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

            foreach (OpenAiPayload.ToolCallAccumulator tool in this.tools.Values.OrderBy(tool => tool.Index))
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

            if (!this.tools.TryGetValue(index, out OpenAiPayload.ToolCallAccumulator? accumulator))
            {
                accumulator = new OpenAiPayload.ToolCallAccumulator(index);
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
