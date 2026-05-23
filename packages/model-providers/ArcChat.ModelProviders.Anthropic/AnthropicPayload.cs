// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.ModelProviders.Anthropic;

internal static class AnthropicPayload
{
    internal sealed class MessageRequest
    {
        public MessageRequest(
            string model,
            ImmutableArray<Message> messages,
            int maxTokens,
            bool stream,
            double temperature,
            double topP,
            int topK,
            ImmutableArray<SystemContentBlock>? system = null,
            ImmutableArray<Tool>? tools = null,
            Thinking? thinking = null)
        {
            this.Model = model;
            this.Messages = messages;
            this.MaxTokens = maxTokens;
            this.Stream = stream;
            this.Temperature = temperature;
            this.TopP = topP;
            this.TopK = topK;
            this.System = system;
            this.Tools = tools;
            this.Thinking = thinking;
        }

        [JsonPropertyName("model")]
        public string Model { get; }

        [JsonPropertyName("messages")]
        public ImmutableArray<Message> Messages { get; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; }

        [JsonPropertyName("stream")]
        public bool Stream { get; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; }

        [JsonPropertyName("top_p")]
        public double TopP { get; }

        [JsonPropertyName("top_k")]
        public int TopK { get; }

        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImmutableArray<SystemContentBlock>? System { get; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImmutableArray<Tool>? Tools { get; }

        [JsonPropertyName("thinking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Thinking? Thinking { get; }
    }

    internal sealed class Message
    {
        public Message(string role, ImmutableArray<ContentBlock> content)
        {
            this.Role = role;
            this.Content = content;
        }

        [JsonPropertyName("role")]
        public string Role { get; }

        [JsonPropertyName("content")]
        public ImmutableArray<ContentBlock> Content { get; }
    }

    internal sealed class SystemContentBlock
    {
        public SystemContentBlock(string text, CacheControl? cacheControl = null)
        {
            this.Text = text;
            this.CacheControl = cacheControl;
        }

        [JsonPropertyName("type")]
        public string Type => "text";

        [JsonPropertyName("text")]
        public string Text { get; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControl? CacheControl { get; }
    }

    internal sealed class ContentBlock
    {
        public ContentBlock(
            string type,
            string? text = null,
            ImageSource? source = null,
            string? id = null,
            string? name = null,
            JsonElement? input = null,
            string? toolUseId = null,
            string? content = null,
            CacheControl? cacheControl = null)
        {
            this.Type = type;
            this.Text = text;
            this.Source = source;
            this.Id = id;
            this.Name = name;
            this.Input = input;
            this.ToolUseId = toolUseId;
            this.Content = content;
            this.CacheControl = cacheControl;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; }

        [JsonPropertyName("source")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageSource? Source { get; }

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; }

        [JsonPropertyName("input")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Input { get; }

        [JsonPropertyName("tool_use_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolUseId { get; }

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Content { get; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControl? CacheControl { get; }
    }

    internal sealed class ImageSource
    {
        public ImageSource(string type, string? mediaType = null, string? data = null, string? url = null)
        {
            this.Type = type;
            this.MediaType = mediaType;
            this.Data = data;
            this.Url = url;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("media_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MediaType { get; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Data { get; }

        [JsonPropertyName("url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Url { get; }
    }

    internal sealed class Tool
    {
        public Tool(string name, string description, JsonElement inputSchema, CacheControl? cacheControl = null)
        {
            this.Name = name;
            this.Description = description;
            this.InputSchema = inputSchema;
            this.CacheControl = cacheControl;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("description")]
        public string Description { get; }

        [JsonPropertyName("input_schema")]
        public JsonElement InputSchema { get; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControl? CacheControl { get; }
    }

    internal sealed class CacheControl
    {
        [JsonPropertyName("type")]
        public string Type => "ephemeral";
    }

    internal sealed class Thinking
    {
        public Thinking(string type, int budgetTokens)
        {
            this.Type = type;
            this.BudgetTokens = budgetTokens;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("budget_tokens")]
        public int BudgetTokens { get; }
    }

    internal sealed class ToolUseAccumulator
    {
        private readonly StringBuilder input = new StringBuilder();

        public ToolUseAccumulator(int index)
        {
            this.Index = index;
        }

        public int Index { get; }

        public string Id { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string Input => this.input.ToString();

        public bool IsStarted => !string.IsNullOrEmpty(this.Id) && !string.IsNullOrEmpty(this.Name);

        public void Start(JsonElement contentBlock)
        {
            if (contentBlock.TryGetProperty("id", out JsonElement idElement) && idElement.ValueKind is JsonValueKind.String)
            {
                this.Id = idElement.GetString() ?? this.Id;
            }

            if (contentBlock.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind is JsonValueKind.String)
            {
                this.Name = nameElement.GetString() ?? this.Name;
            }

            if (contentBlock.TryGetProperty("input", out JsonElement inputElement) && inputElement.ValueKind is not JsonValueKind.Null)
            {
                string rawInput = inputElement.GetRawText();
                if (!string.Equals(rawInput, "{}", StringComparison.Ordinal))
                {
                    _ = this.input.Append(rawInput);
                }
            }
        }

        public void AppendInput(string? partialJson)
        {
            if (!string.IsNullOrEmpty(partialJson))
            {
                _ = this.input.Append(partialJson);
            }
        }
    }
}
