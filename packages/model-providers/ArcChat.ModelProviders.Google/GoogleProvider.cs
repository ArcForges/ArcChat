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

namespace ArcChat.ModelProviders.Google;

/// <summary>
/// Google Gemini streamGenerateContent chat provider.
/// </summary>
public sealed class GoogleProvider : IChatProvider
{
    private const int DefaultMaxOutputTokens = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly ServerSentEventReader SseReader = new ServerSentEventReader();

    private readonly HttpClient httpClient;
    private readonly GoogleProviderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleProvider"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for provider requests.</param>
    /// <param name="options">Optional Google provider options.</param>
    public GoogleProvider(HttpClient httpClient, GoogleProviderOptions? options = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? new GoogleProviderOptions();
    }

    /// <inheritdoc />
    public ProviderId Id => new ProviderId("Google");

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
        GoogleStreamState state = new GoogleStreamState(request);
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
        return modelId.Contains("-thinking", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gemini-2.5", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsVisionModel(string modelId)
    {
        return modelId.Contains("gemini-1.5", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gemini-exp", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gemini-2.0", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("gemini-2.5", StringComparison.OrdinalIgnoreCase)
            || modelId.Contains("learnlm", StringComparison.OrdinalIgnoreCase);
    }

    internal static string ToApiValue(GoogleSafetySettingsThreshold threshold)
    {
        return threshold switch
        {
            GoogleSafetySettingsThreshold.BlockNone => "BLOCK_NONE",
            GoogleSafetySettingsThreshold.BlockOnlyHigh => "BLOCK_ONLY_HIGH",
            GoogleSafetySettingsThreshold.BlockMediumAndAbove => "BLOCK_MEDIUM_AND_ABOVE",
            GoogleSafetySettingsThreshold.BlockLowAndAbove => "BLOCK_LOW_AND_ABOVE",
            _ => "BLOCK_ONLY_HIGH",
        };
    }

    private static GooglePayload.GenerateContentRequest CreatePayload(ChatRequest request, GoogleSafetySettingsThreshold safetyThreshold)
    {
        bool visionModel = IsVisionModel(request.Config.Model);
        GooglePayload.Content? systemInstruction = CreateSystemInstruction(request.History);
        ImmutableArray<GooglePayload.Content> contents = CreateContents(request.History, visionModel);
        ImmutableArray<GooglePayload.Tool>? tools = request.Tools.IsDefaultOrEmpty
            ? null
            : ImmutableArray.Create(new GooglePayload.Tool(CreateFunctionDeclarations(request.Tools)));

        return new GooglePayload.GenerateContentRequest(
            contents,
            new GooglePayload.GenerationConfig(
                request.Config.Temperature,
                request.Config.TopP,
                request.Config.MaxTokens > 0 ? request.Config.MaxTokens : DefaultMaxOutputTokens),
            CreateSafetySettings(safetyThreshold),
            systemInstruction,
            tools);
    }

    private static GooglePayload.Content? CreateSystemInstruction(ImmutableArray<Message> history)
    {
        string text = string.Join(
            "\n\n",
            history
                .Where(message => message.Role is MessageRole.System)
                .Select(GetTextContent)
                .Where(content => !string.IsNullOrWhiteSpace(content)));
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new GooglePayload.Content(
            "user",
            ImmutableArray.Create(new GooglePayload.Part(text: text)));
    }

    private static ImmutableArray<GooglePayload.Content> CreateContents(ImmutableArray<Message> history, bool visionModel)
    {
        ImmutableArray<GooglePayload.Content>.Builder contents = ImmutableArray.CreateBuilder<GooglePayload.Content>();
        foreach (Message message in history.Where(message => message.Role is not MessageRole.System))
        {
            string role = message.Role is MessageRole.Assistant ? "model" : "user";
            ImmutableArray<GooglePayload.Part> parts = CreateParts(message, visionModel);
            if (parts.IsDefaultOrEmpty)
            {
                continue;
            }

            if (contents.Count == 0 && string.Equals(role, "model", StringComparison.Ordinal))
            {
                contents.Add(CreateFillerContent("user"));
            }

            if (contents.Count > 0 && string.Equals(contents[^1].Role, role, StringComparison.Ordinal))
            {
                contents[^1].AddParts(parts);
            }
            else
            {
                contents.Add(new GooglePayload.Content(role, parts));
            }
        }

        if (contents.Count == 0)
        {
            contents.Add(CreateFillerContent("user"));
        }

        return contents.ToImmutable();
    }

    private static GooglePayload.Content CreateFillerContent(string role)
    {
        return new GooglePayload.Content(
            role,
            ImmutableArray.Create(new GooglePayload.Part(text: ";")));
    }

    private static ImmutableArray<GooglePayload.Part> CreateParts(Message message, bool visionModel)
    {
        ImmutableArray<GooglePayload.Part>.Builder parts = ImmutableArray.CreateBuilder<GooglePayload.Part>();
        string text = GetTextContent(message);
        if (!string.IsNullOrEmpty(text))
        {
            parts.Add(new GooglePayload.Part(text: text));
        }

        if (visionModel)
        {
            foreach (ImageBlock image in message.Content.OfType<ImageBlock>())
            {
                if (TryParseDataUrl(image.Url, out string? mimeType, out string? data)
                    && mimeType is not null
                    && data is not null)
                {
                    parts.Add(new GooglePayload.Part(inlineData: new GooglePayload.InlineData(mimeType, data)));
                }
            }
        }

        foreach (ToolCallBlock toolCall in message.Content.OfType<ToolCallBlock>())
        {
            parts.Add(new GooglePayload.Part(functionCall: new GooglePayload.FunctionCall(toolCall.Name, toolCall.Arguments, toolCall.Id)));
        }

        if (message.Role is MessageRole.Assistant && !message.Tools.IsDefaultOrEmpty)
        {
            foreach (ChatMessageTool tool in message.Tools)
            {
                parts.Add(new GooglePayload.Part(functionCall: new GooglePayload.FunctionCall(
                    tool.Name,
                    CreateJsonElement(tool.Arguments ?? string.Empty),
                    tool.Id)));
            }
        }

        foreach (ToolResultBlock toolResult in message.Content.OfType<ToolResultBlock>())
        {
            parts.Add(new GooglePayload.Part(functionResponse: new GooglePayload.FunctionResponse(
                toolResult.Name,
                CreateFunctionResponse(toolResult),
                toolResult.ToolCallId)));
        }

        return parts.ToImmutable();
    }

    private static ImmutableArray<GooglePayload.FunctionDeclaration> CreateFunctionDeclarations(ImmutableArray<ArcTool> tools)
    {
        ImmutableArray<GooglePayload.FunctionDeclaration>.Builder builder = ImmutableArray.CreateBuilder<GooglePayload.FunctionDeclaration>(tools.Length);
        foreach (ArcTool tool in tools)
        {
            builder.Add(new GooglePayload.FunctionDeclaration(tool.Name, tool.Description, tool.InputSchema));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<GooglePayload.SafetySetting> CreateSafetySettings(GoogleSafetySettingsThreshold threshold)
    {
        string apiValue = ToApiValue(threshold);
        return ImmutableArray.Create(
            new GooglePayload.SafetySetting("HARM_CATEGORY_HARASSMENT", apiValue),
            new GooglePayload.SafetySetting("HARM_CATEGORY_HATE_SPEECH", apiValue),
            new GooglePayload.SafetySetting("HARM_CATEGORY_SEXUALLY_EXPLICIT", apiValue),
            new GooglePayload.SafetySetting("HARM_CATEGORY_DANGEROUS_CONTENT", apiValue));
    }

    private static JsonElement CreateFunctionResponse(ToolResultBlock toolResult)
    {
        return JsonSerializer.SerializeToElement(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = toolResult.Name,
                ["content"] = toolResult.Content,
            },
            JsonOptions);
    }

    private static JsonElement CreateJsonElement(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.SerializeToElement(new Dictionary<string, string>(StringComparer.Ordinal), JsonOptions);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(json, JsonOptions);
        }
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
        GooglePayload.GenerateContentRequest payload = CreatePayload(request, this.options.SafetyThreshold);
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.CreateStreamUri(request.Config.Model))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (!string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            _ = httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", this.options.ApiKey);
        }

        return httpRequest;
    }

    private Uri CreateStreamUri(string modelId)
    {
        string baseUri = this.options.BaseUri.ToString().TrimEnd('/');
        string escapedModel = Uri.EscapeDataString(modelId);
        return new Uri($"{baseUri}/v1beta/models/{escapedModel}:streamGenerateContent?alt=sse", UriKind.Absolute);
    }

    private sealed class GoogleStreamState
    {
        private readonly ChatRequest request;
        private readonly StringBuilder content = new StringBuilder();
        private readonly List<ChatMessageTool> tools = new List<ChatMessageTool>();

        public GoogleStreamState(ChatRequest request)
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
            if (document.RootElement.TryGetProperty("promptFeedback", out JsonElement promptFeedback)
                && promptFeedback.TryGetProperty("blockReason", out JsonElement blockReasonElement)
                && blockReasonElement.ValueKind is JsonValueKind.String)
            {
                string blockReason = blockReasonElement.GetString() ?? "BLOCKED";
                yield return new ChatError(this.request.Extra.ConversationId, this.request.Extra.MessageId, "PromptBlocked", blockReason);
                foreach (ChatEvent chatEvent in this.Complete("error"))
                {
                    yield return chatEvent;
                }

                yield break;
            }

            if (!document.RootElement.TryGetProperty("candidates", out JsonElement candidates)
                || candidates.ValueKind is not JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                yield break;
            }

            JsonElement candidate = candidates[0];
            if (candidate.TryGetProperty("content", out JsonElement candidateContent)
                && candidateContent.TryGetProperty("parts", out JsonElement parts)
                && parts.ValueKind is JsonValueKind.Array)
            {
                foreach (ChatEvent chatEvent in this.ApplyParts(parts))
                {
                    yield return chatEvent;
                }
            }

            if (candidate.TryGetProperty("finishReason", out JsonElement finishReasonElement)
                && finishReasonElement.ValueKind is JsonValueKind.String)
            {
                string? finishReason = finishReasonElement.GetString();
                if (!string.IsNullOrEmpty(finishReason) && !string.Equals(finishReason, "FINISH_REASON_UNSPECIFIED", StringComparison.Ordinal))
                {
                    foreach (ChatEvent chatEvent in this.Complete(finishReason))
                    {
                        yield return chatEvent;
                    }
                }
            }
        }

        public IEnumerable<ChatEvent> Complete(string? finishReason)
        {
            if (this.IsComplete)
            {
                yield break;
            }

            Message message = new Message(
                this.request.Extra.MessageId,
                MessageRole.Assistant,
                ImmutableArray.Create<ContentBlock>(new TextBlock(this.content.ToString())),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                Model: this.request.Config.Model,
                Tools: this.tools.ToImmutableArray());

            yield return new MessageCompleted(this.request.Extra.ConversationId, this.request.Extra.MessageId, message);
            yield return new ChatFinished(this.request.Extra.ConversationId, this.request.Extra.MessageId, finishReason);
            this.IsComplete = true;
        }

        private static ChatMessageTool CreateTool(JsonElement functionCall, int index)
        {
            string id = functionCall.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind is JsonValueKind.String
                ? idElement.GetString() ?? $"call_{index.ToString(CultureInfo.InvariantCulture)}"
                : $"call_{index.ToString(CultureInfo.InvariantCulture)}";
            string name = functionCall.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind is JsonValueKind.String
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            string arguments = "{}";
            if (functionCall.TryGetProperty("args", out JsonElement argsElement) && argsElement.ValueKind is not JsonValueKind.Null)
            {
                arguments = argsElement.GetRawText();
            }

            return new ChatMessageTool(id, name, arguments, index, "function");
        }

        private IEnumerable<ChatEvent> ApplyParts(JsonElement parts)
        {
            foreach (JsonElement part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out JsonElement textElement) && textElement.ValueKind is JsonValueKind.String)
                {
                    string? text = textElement.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _ = this.content.Append(text);
                        yield return new MessageDelta(this.request.Extra.ConversationId, this.request.Extra.MessageId, text);
                    }
                }

                if (part.TryGetProperty("functionCall", out JsonElement functionCall))
                {
                    ChatMessageTool tool = CreateTool(functionCall, this.tools.Count);
                    this.tools.Add(tool);
                    yield return new ToolCallStarted(this.request.Extra.ConversationId, this.request.Extra.MessageId, tool);
                    yield return new ToolCallCompleted(this.request.Extra.ConversationId, this.request.Extra.MessageId, tool, CreateJsonElement(tool.Arguments ?? string.Empty));
                }
            }
        }
    }
}
