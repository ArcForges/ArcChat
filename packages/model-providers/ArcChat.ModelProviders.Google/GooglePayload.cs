// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.ModelProviders.Google;

internal static class GooglePayload
{
    internal sealed class GenerateContentRequest
    {
        public GenerateContentRequest(
            ImmutableArray<Content> contents,
            GenerationConfig generationConfig,
            ImmutableArray<SafetySetting> safetySettings,
            Content? systemInstruction = null,
            ImmutableArray<Tool>? tools = null)
        {
            this.Contents = contents;
            this.GenerationConfig = generationConfig;
            this.SafetySettings = safetySettings;
            this.SystemInstruction = systemInstruction;
            this.Tools = tools;
        }

        [JsonPropertyName("contents")]
        public ImmutableArray<Content> Contents { get; }

        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; }

        [JsonPropertyName("safetySettings")]
        public ImmutableArray<SafetySetting> SafetySettings { get; }

        [JsonPropertyName("systemInstruction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Content? SystemInstruction { get; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImmutableArray<Tool>? Tools { get; }
    }

    internal sealed class GenerationConfig
    {
        public GenerationConfig(double temperature, double topP, int maxOutputTokens)
        {
            this.Temperature = temperature;
            this.TopP = topP;
            this.MaxOutputTokens = maxOutputTokens;
        }

        [JsonPropertyName("temperature")]
        public double Temperature { get; }

        [JsonPropertyName("topP")]
        public double TopP { get; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; }
    }

    internal sealed class SafetySetting
    {
        public SafetySetting(string category, string threshold)
        {
            this.Category = category;
            this.Threshold = threshold;
        }

        [JsonPropertyName("category")]
        public string Category { get; }

        [JsonPropertyName("threshold")]
        public string Threshold { get; }
    }

    internal sealed class Content
    {
        public Content(string role, ImmutableArray<Part> parts)
        {
            this.Role = role;
            this.Parts = parts;
        }

        [JsonPropertyName("role")]
        public string Role { get; }

        [JsonPropertyName("parts")]
        public ImmutableArray<Part> Parts { get; private set; }

        public void AddParts(ImmutableArray<Part> parts)
        {
            this.Parts = this.Parts.AddRange(parts);
        }
    }

    internal sealed class Part
    {
        public Part(
            string? text = null,
            InlineData? inlineData = null,
            FunctionCall? functionCall = null,
            FunctionResponse? functionResponse = null)
        {
            this.Text = text;
            this.InlineData = inlineData;
            this.FunctionCall = functionCall;
            this.FunctionResponse = functionResponse;
        }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlineData? InlineData { get; }

        [JsonPropertyName("functionCall")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FunctionCall? FunctionCall { get; }

        [JsonPropertyName("functionResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FunctionResponse? FunctionResponse { get; }
    }

    internal sealed class InlineData
    {
        public InlineData(string mimeType, string data)
        {
            this.MimeType = mimeType;
            this.Data = data;
        }

        [JsonPropertyName("mimeType")]
        public string MimeType { get; }

        [JsonPropertyName("data")]
        public string Data { get; }
    }

    internal sealed class FunctionCall
    {
        public FunctionCall(string name, JsonElement args, string? id = null)
        {
            this.Name = name;
            this.Args = args;
            this.Id = id;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("args")]
        public JsonElement Args { get; }

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; }
    }

    internal sealed class FunctionResponse
    {
        public FunctionResponse(string name, JsonElement response, string? id = null)
        {
            this.Name = name;
            this.Response = response;
            this.Id = id;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("response")]
        public JsonElement Response { get; }

        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; }
    }

    internal sealed class Tool
    {
        public Tool(ImmutableArray<FunctionDeclaration> functionDeclarations)
        {
            this.FunctionDeclarations = functionDeclarations;
        }

        [JsonPropertyName("functionDeclarations")]
        public ImmutableArray<FunctionDeclaration> FunctionDeclarations { get; }
    }

    internal sealed class FunctionDeclaration
    {
        public FunctionDeclaration(string name, string description, JsonElement parameters)
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
}
