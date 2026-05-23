// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.ModelProviders.OpenAi;

internal static class OpenAiPayload
{
    internal sealed class ChatRequest
    {
        public ChatRequest(
            string model,
            ImmutableArray<Message> messages,
            bool stream,
            double temperature,
            double presencePenalty,
            double frequencyPenalty,
            double topP,
            int? maxTokens = null,
            int? maxCompletionTokens = null,
            ImmutableArray<Tool>? tools = null)
        {
            this.Model = model;
            this.Messages = messages;
            this.Stream = stream;
            this.Temperature = temperature;
            this.PresencePenalty = presencePenalty;
            this.FrequencyPenalty = frequencyPenalty;
            this.TopP = topP;
            this.MaxTokens = maxTokens;
            this.MaxCompletionTokens = maxCompletionTokens;
            this.Tools = tools;
        }

        [JsonPropertyName("model")]
        public string Model { get; }

        [JsonPropertyName("messages")]
        public ImmutableArray<Message> Messages { get; }

        [JsonPropertyName("stream")]
        public bool Stream { get; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; }

        [JsonPropertyName("presence_penalty")]
        public double PresencePenalty { get; }

        [JsonPropertyName("frequency_penalty")]
        public double FrequencyPenalty { get; }

        [JsonPropertyName("top_p")]
        public double TopP { get; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; }

        [JsonPropertyName("max_completion_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxCompletionTokens { get; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImmutableArray<Tool>? Tools { get; }
    }

    internal sealed class Message
    {
        public Message(string role, JsonElement content, string? toolCallId = null)
        {
            this.Role = role;
            this.Content = content;
            this.ToolCallId = toolCallId;
        }

        [JsonPropertyName("role")]
        public string Role { get; }

        [JsonPropertyName("content")]
        public JsonElement Content { get; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolCallId { get; }
    }

    internal sealed class ContentPart
    {
        public ContentPart(string type, string? text = null, ImageUrl? imageUrl = null)
        {
            this.Type = type;
            this.Text = text;
            this.ImageUrl = imageUrl;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; }

        [JsonPropertyName("image_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageUrl? ImageUrl { get; }
    }

    internal sealed class ImageUrl
    {
        public ImageUrl(string url, string? detail = null)
        {
            this.Url = url;
            this.Detail = detail;
        }

        [JsonPropertyName("url")]
        public string Url { get; }

        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; }
    }

    internal sealed class Tool
    {
        public Tool(string type, FunctionTool function)
        {
            this.Type = type;
            this.Function = function;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("function")]
        public FunctionTool Function { get; }
    }

    internal sealed class FunctionTool
    {
        public FunctionTool(string name, string description, JsonElement parameters)
        {
            this.Name = name;
            this.Description = description;
            this.Parameters = parameters;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("description")]
        public string Description { get; }

        [JsonPropertyName("parameters")]
        public JsonElement Parameters { get; }
    }

    internal sealed class ToolCallAccumulator
    {
        private readonly StringBuilder arguments = new StringBuilder();

        public ToolCallAccumulator(int index)
        {
            this.Index = index;
        }

        public int Index { get; }

        public string Id { get; private set; } = string.Empty;

        public string Type { get; private set; } = "function";

        public string Name { get; private set; } = string.Empty;

        public string Arguments => this.arguments.ToString();

        public bool IsStarted => !string.IsNullOrEmpty(this.Id) && !string.IsNullOrEmpty(this.Name);

        public void Apply(JsonElement toolCall)
        {
            if (toolCall.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind is JsonValueKind.String)
            {
                this.Id = idElement.GetString() ?? this.Id;
            }

            if (toolCall.TryGetProperty("type", out JsonElement typeElement) && typeElement.ValueKind is JsonValueKind.String)
            {
                this.Type = typeElement.GetString() ?? this.Type;
            }

            if (toolCall.TryGetProperty("function", out JsonElement functionElement))
            {
                this.ApplyFunction(functionElement);
            }
        }

        private void ApplyFunction(JsonElement functionElement)
        {
            if (functionElement.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind is JsonValueKind.String)
            {
                this.Name = nameElement.GetString() ?? this.Name;
            }

            if (functionElement.TryGetProperty("arguments", out JsonElement argumentsElement)
                && argumentsElement.ValueKind is JsonValueKind.String)
            {
                _ = this.arguments.Append(argumentsElement.GetString());
            }
        }
    }
}
