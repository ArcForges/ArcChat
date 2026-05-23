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

namespace ArcChat.ModelProviders.Anthropic;

/// <summary>
/// Anthropic Messages API chat provider.
/// </summary>
public sealed class AnthropicProvider : IChatProvider
{
    private const int DefaultMaxTokens = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly ServerSentEventReader SseReader = new ServerSentEventReader();

    private readonly HttpClient httpClient;
    private readonly AnthropicProviderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="options">Optional Anthropic provider options.</param>
    public AnthropicProvider(HttpClient httpClient, AnthropicProviderOptions? options = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? new AnthropicProviderOptions();
    }

    /// <inheritdoc />
    public ProviderId Id => new ProviderId("Anthropic");

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
        AnthropicStreamState state = new AnthropicStreamState(request);
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

        foreach (ChatEvent chatEvent in state.Complete())
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
        return modelId.Contains("claude-3-7", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("claude-sonnet-4", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("claude-opus-4", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVisionModel(string modelId)
    {
        return (modelId.Contains("claude-3", StringComparison.OrdinalIgnoreCase)
                || modelId.Contains("claude-4", StringComparison.OrdinalIgnoreCase)
                || modelId.Contains("claude-sonnet-4", StringComparison.OrdinalIgnoreCase)
                || modelId.Contains("claude-opus-4", StringComparison.OrdinalIgnoreCase))
            && !modelId.Equals("claude-3-5-haiku-20241022", StringComparison.OrdinalIgnoreCase);
    }

    private static AnthropicPayload.MessageRequest CreatePayload(ChatRequest request)
    {
        bool visionModel = IsVisionModel(request.Config.Model);
        ImmutableArray<AnthropicPayload.SystemContentBlock>? system = CreateSystem(request.History);
        ImmutableArray<AnthropicPayload.Message> messages = CreateMessages(request.History, visionModel);
        ImmutableArray<AnthropicPayload.Tool>? tools = request.Tools.IsDefaultOrEmpty
            ? null
            : CreateTools(request.Tools);

        return new AnthropicPayload.MessageRequest(
            request.Config.Model,
            messages,
            request.Config.MaxTokens > 0 ? request.Config.MaxTokens : DefaultMaxTokens,
            true,
            request.Config.Temperature,
            request.Config.TopP,
            5,
            system,
            tools,
            CreateThinking(request));
    }

    private static ImmutableArray<AnthropicPayload.SystemContentBlock>? CreateSystem(ImmutableArray<Message> history)
    {
        ImmutableArray<string> systemTexts = history
            .Where(message => message.Role is MessageRole.System)
            .Select(GetTextContent)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToImmutableArray();
        if (systemTexts.IsDefaultOrEmpty)
        {
            return null;
        }

        ImmutableArray<AnthropicPayload.SystemContentBlock>.Builder blocks = ImmutableArray.CreateBuilder<AnthropicPayload.SystemContentBlock>(systemTexts.Length);
        for (int index = 0; index < systemTexts.Length; index++)
        {
            AnthropicPayload.CacheControl? cacheControl = index == systemTexts.Length - 1 ? new AnthropicPayload.CacheControl() : null;
            blocks.Add(new AnthropicPayload.SystemContentBlock(systemTexts[index], cacheControl));
        }

        return blocks.ToImmutable();
    }

    private static ImmutableArray<AnthropicPayload.Message> CreateMessages(ImmutableArray<Message> history, bool visionModel)
    {
        ImmutableArray<AnthropicPayload.Message>.Builder messages = ImmutableArray.CreateBuilder<AnthropicPayload.Message>();
        string? previousRole = null;
        foreach (Message message in history.Where(message => message.Role is not MessageRole.System))
        {
            string role = message.Role is MessageRole.Assistant ? "assistant" : "user";
            ImmutableArray<AnthropicPayload.ContentBlock> content = CreateContentBlocks(message, visionModel);
            if (content.IsDefaultOrEmpty)
            {
                continue;
            }

            if (messages.Count == 0 && string.Equals(role, "assistant", StringComparison.Ordinal))
            {
                messages.Add(CreateFillerMessage("user"));
                previousRole = "user";
            }

            if (string.Equals(previousRole, role, StringComparison.Ordinal))
            {
                string fillerRole = string.Equals(role, "user", StringComparison.Ordinal) ? "assistant" : "user";
                messages.Add(CreateFillerMessage(fillerRole));
            }

            messages.Add(new AnthropicPayload.Message(role, content));
            previousRole = role;
        }

        if (messages.Count == 0)
        {
            messages.Add(CreateFillerMessage("user"));
        }

        return messages.ToImmutable();
    }

    private static AnthropicPayload.Message CreateFillerMessage(string role)
    {
        return new AnthropicPayload.Message(
            role,
            ImmutableArray.Create(new AnthropicPayload.ContentBlock("text", text: ";")));
    }

    private static ImmutableArray<AnthropicPayload.ContentBlock> CreateContentBlocks(Message message, bool visionModel)
    {
        ImmutableArray<AnthropicPayload.ContentBlock>.Builder blocks = ImmutableArray.CreateBuilder<AnthropicPayload.ContentBlock>();
        string text = GetTextContent(message);
        if (!string.IsNullOrEmpty(text))
        {
            blocks.Add(new AnthropicPayload.ContentBlock("text", text: text));
        }

        if (visionModel)
        {
            foreach (ImageBlock image in message.Content.OfType<ImageBlock>())
            {
                blocks.Add(new AnthropicPayload.ContentBlock("image", source: CreateImageSource(image.Url)));
            }
        }

        foreach (ToolCallBlock toolCall in message.Content.OfType<ToolCallBlock>())
        {
            blocks.Add(CreateToolUseBlock(toolCall));
        }

        if (message.Role is MessageRole.Assistant && !message.Tools.IsDefaultOrEmpty)
        {
            foreach (ChatMessageTool tool in message.Tools)
            {
                blocks.Add(CreateToolUseBlock(tool));
            }
        }

        foreach (ToolResultBlock toolResult in message.Content.OfType<ToolResultBlock>())
        {
            blocks.Add(new AnthropicPayload.ContentBlock(
                "tool_result",
                toolUseId: toolResult.ToolCallId,
                content: toolResult.Content));
        }

        return blocks.ToImmutable();
    }

    private static AnthropicPayload.ContentBlock CreateToolUseBlock(ToolCallBlock toolCall)
    {
        return new AnthropicPayload.ContentBlock(
            "tool_use",
            id: toolCall.Id,
            name: toolCall.Name,
            input: toolCall.Arguments);
    }

    private static AnthropicPayload.ContentBlock CreateToolUseBlock(ChatMessageTool tool)
    {
        return new AnthropicPayload.ContentBlock(
            "tool_use",
            id: tool.Id,
            name: tool.Name,
            input: CreateToolInput(tool.Arguments ?? string.Empty));
    }

    private static JsonElement CreateToolInput(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return JsonSerializer.SerializeToElement(new Dictionary<string, string>(StringComparer.Ordinal), JsonOptions);
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

    private static ImmutableArray<AnthropicPayload.Tool> CreateTools(ImmutableArray<ArcTool> tools)
    {
        ImmutableArray<AnthropicPayload.Tool>.Builder builder = ImmutableArray.CreateBuilder<AnthropicPayload.Tool>(tools.Length);
        for (int index = 0; index < tools.Length; index++)
        {
            ArcTool tool = tools[index];
            AnthropicPayload.CacheControl? cacheControl = index == tools.Length - 1 ? new AnthropicPayload.CacheControl() : null;
            builder.Add(new AnthropicPayload.Tool(tool.Name, tool.Description, tool.InputSchema, cacheControl));
        }

        return builder.ToImmutable();
    }

    private static AnthropicPayload.Thinking? CreateThinking(ChatRequest request)
    {
        if (!IsReasoningModel(request.Config.Model)
            || request.Config.Extra is null
            || !request.Config.Extra.TryGetValue("anthropic_thinking_budget_tokens", out JsonElement budgetElement)
            || !budgetElement.TryGetInt32(out int budget)
            || budget <= 0)
        {
            return null;
        }

        return new AnthropicPayload.Thinking("enabled", Math.Max(1024, budget));
    }

    private static AnthropicPayload.ImageSource CreateImageSource(string url)
    {
        if (TryParseDataUrl(url, out string? mediaType, out string? data))
        {
            return new AnthropicPayload.ImageSource("base64", mediaType, data);
        }

        return new AnthropicPayload.ImageSource("url", url: url);
    }

    private static bool TryParseDataUrl(string url, out string? mediaType, out string? data)
    {
        mediaType = null;
        data = null;
        if (!url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int semicolonIndex = url.IndexOf(';', StringComparison.Ordinal);
        int commaIndex = url.IndexOf(',', StringComparison.Ordinal);
        if (semicolonIndex < 0 || commaIndex < semicolonIndex)
        {
            return false;
        }

        string encoding = url.Substring(semicolonIndex + 1, commaIndex - semicolonIndex - 1);
        if (!encoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        mediaType = url[5..semicolonIndex];
        data = url.Substring(commaIndex + 1);
        return !string.IsNullOrWhiteSpace(mediaType) && !string.IsNullOrEmpty(data);
    }

    private static string GetTextContent(Message message)
    {
        return string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    private HttpRequestMessage CreateHttpRequest(ChatRequest request)
    {
        AnthropicPayload.MessageRequest payload = CreatePayload(request);
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.CreateMessagesUri())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        _ = httpRequest.Headers.TryAddWithoutValidation("anthropic-version", this.options.ApiVersion);
        if (!string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            _ = httpRequest.Headers.TryAddWithoutValidation("x-api-key", this.options.ApiKey);
        }

        return httpRequest;
    }

    private Uri CreateMessagesUri()
    {
        string baseUri = this.options.BaseUri.ToString().TrimEnd('/');
        return new Uri($"{baseUri}/v1/messages", UriKind.Absolute);
    }

    private sealed class AnthropicStreamState
    {
        private readonly ChatRequest request;
        private readonly StringBuilder content = new StringBuilder();
        private readonly Dictionary<int, AnthropicPayload.ToolUseAccumulator> tools = new Dictionary<int, AnthropicPayload.ToolUseAccumulator>();
        private readonly HashSet<int> completedToolIndexes = new HashSet<int>();
        private string? finishReason;

        public AnthropicStreamState(ChatRequest request)
        {
            this.request = request;
        }

        public bool IsComplete { get; private set; }

        public IEnumerable<ChatEvent> Apply(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                yield break;
            }

            using JsonDocument document = JsonDocument.Parse(data);
            string? type = null;
            if (document.RootElement.TryGetProperty("type", out JsonElement typeElement) && typeElement.ValueKind is JsonValueKind.String)
            {
                type = typeElement.GetString();
            }

            switch (type)
            {
                case "content_block_start":
                    foreach (ChatEvent chatEvent in this.ApplyContentBlockStart(document.RootElement))
                    {
                        yield return chatEvent;
                    }

                    break;
                case "content_block_delta":
                    foreach (ChatEvent chatEvent in this.ApplyContentBlockDelta(document.RootElement))
                    {
                        yield return chatEvent;
                    }

                    break;
                case "content_block_stop":
                    foreach (ChatEvent chatEvent in this.ApplyContentBlockStop(document.RootElement))
                    {
                        yield return chatEvent;
                    }

                    break;
                case "message_delta":
                    this.ApplyMessageDelta(document.RootElement);
                    break;
                case "message_stop":
                    foreach (ChatEvent chatEvent in this.Complete())
                    {
                        yield return chatEvent;
                    }

                    break;
                case "error":
                    foreach (ChatEvent chatEvent in this.ApplyError(document.RootElement))
                    {
                        yield return chatEvent;
                    }

                    break;
            }
        }

        public IEnumerable<ChatEvent> Complete()
        {
            if (this.IsComplete)
            {
                yield break;
            }

            foreach (AnthropicPayload.ToolUseAccumulator tool in this.tools.Values
                .OrderBy(tool => tool.Index)
                .Where(tool => this.completedToolIndexes.Add(tool.Index)))
            {
                yield return CreateToolCompleted(this.request, tool);
            }

            Message message = new Message(
                this.request.Extra.MessageId,
                MessageRole.Assistant,
                ImmutableArray.Create<ContentBlock>(new TextBlock(this.content.ToString())),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                Model: this.request.Config.Model,
                Tools: this.tools.Values
                    .OrderBy(tool => tool.Index)
                    .Select(tool => new ChatMessageTool(tool.Id, tool.Name, tool.Input, tool.Index, "function"))
                    .ToImmutableArray());

            yield return new MessageCompleted(this.request.Extra.ConversationId, this.request.Extra.MessageId, message);
            yield return new ChatFinished(this.request.Extra.ConversationId, this.request.Extra.MessageId, this.finishReason);
            this.IsComplete = true;
        }

        private static int GetIndex(JsonElement root)
        {
            return root.TryGetProperty("index", out JsonElement indexElement) && indexElement.TryGetInt32(out int index)
                ? index
                : 0;
        }

        private static ToolCallCompleted CreateToolCompleted(ChatRequest request, AnthropicPayload.ToolUseAccumulator tool)
        {
            return new ToolCallCompleted(
                request.Extra.ConversationId,
                request.Extra.MessageId,
                new ChatMessageTool(tool.Id, tool.Name, tool.Input, tool.Index, "function"),
                CreateToolResult(tool.Input));
        }

        private static JsonElement CreateToolResult(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return JsonSerializer.SerializeToElement(new Dictionary<string, string>(StringComparer.Ordinal), JsonOptions);
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

        private IEnumerable<ChatEvent> ApplyContentBlockStart(JsonElement root)
        {
            int index = GetIndex(root);
            if (!root.TryGetProperty("content_block", out JsonElement contentBlock)
                || !contentBlock.TryGetProperty("type", out JsonElement typeElement)
                || !string.Equals(typeElement.GetString(), "tool_use", StringComparison.Ordinal))
            {
                yield break;
            }

            if (!this.tools.TryGetValue(index, out AnthropicPayload.ToolUseAccumulator? accumulator))
            {
                accumulator = new AnthropicPayload.ToolUseAccumulator(index);
                this.tools.Add(index, accumulator);
            }

            accumulator.Start(contentBlock);
            if (accumulator.IsStarted)
            {
                yield return new ToolCallStarted(
                    this.request.Extra.ConversationId,
                    this.request.Extra.MessageId,
                    new ChatMessageTool(accumulator.Id, accumulator.Name, accumulator.Input, accumulator.Index, "function"));
            }
        }

        private IEnumerable<ChatEvent> ApplyContentBlockDelta(JsonElement root)
        {
            if (!root.TryGetProperty("delta", out JsonElement delta)
                || !delta.TryGetProperty("type", out JsonElement typeElement)
                || typeElement.ValueKind is not JsonValueKind.String)
            {
                yield break;
            }

            string? type = typeElement.GetString();
            if (string.Equals(type, "text_delta", StringComparison.Ordinal)
                && delta.TryGetProperty("text", out JsonElement textElement)
                && textElement.ValueKind is JsonValueKind.String)
            {
                string? text = textElement.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    _ = this.content.Append(text);
                    yield return new MessageDelta(this.request.Extra.ConversationId, this.request.Extra.MessageId, text);
                }
            }
            else if (string.Equals(type, "thinking_delta", StringComparison.Ordinal)
                && delta.TryGetProperty("thinking", out JsonElement thinkingElement)
                && thinkingElement.ValueKind is JsonValueKind.String)
            {
                string? thinking = thinkingElement.GetString();
                if (!string.IsNullOrEmpty(thinking))
                {
                    yield return new ReasoningDelta(this.request.Extra.ConversationId, this.request.Extra.MessageId, thinking);
                }
            }
            else if (string.Equals(type, "input_json_delta", StringComparison.Ordinal)
                && delta.TryGetProperty("partial_json", out JsonElement partialElement)
                && partialElement.ValueKind is JsonValueKind.String)
            {
                int index = GetIndex(root);
                if (this.tools.TryGetValue(index, out AnthropicPayload.ToolUseAccumulator? accumulator))
                {
                    accumulator.AppendInput(partialElement.GetString());
                }
            }
        }

        private IEnumerable<ChatEvent> ApplyContentBlockStop(JsonElement root)
        {
            int index = GetIndex(root);
            if (this.tools.TryGetValue(index, out AnthropicPayload.ToolUseAccumulator? accumulator)
                && this.completedToolIndexes.Add(index))
            {
                yield return CreateToolCompleted(this.request, accumulator);
            }
        }

        private void ApplyMessageDelta(JsonElement root)
        {
            if (root.TryGetProperty("delta", out JsonElement delta)
                && delta.TryGetProperty("stop_reason", out JsonElement stopReasonElement)
                && stopReasonElement.ValueKind is JsonValueKind.String)
            {
                this.finishReason = stopReasonElement.GetString();
            }
        }

        private IEnumerable<ChatEvent> ApplyError(JsonElement root)
        {
            string code = "AnthropicStreamError";
            string message = "Anthropic stream error.";
            if (root.TryGetProperty("error", out JsonElement error))
            {
                if (error.TryGetProperty("type", out JsonElement codeElement) && codeElement.ValueKind is JsonValueKind.String)
                {
                    code = codeElement.GetString() ?? code;
                }

                if (error.TryGetProperty("message", out JsonElement messageElement) && messageElement.ValueKind is JsonValueKind.String)
                {
                    message = messageElement.GetString() ?? message;
                }
            }

            yield return new ChatError(this.request.Extra.ConversationId, this.request.Extra.MessageId, code, message);
            this.finishReason = "error";
            foreach (ChatEvent chatEvent in this.Complete())
            {
                yield return chatEvent;
            }
        }
    }
}
