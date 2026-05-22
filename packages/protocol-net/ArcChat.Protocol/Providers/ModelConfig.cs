// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;

namespace ArcChat.Protocol.Providers;

/// <summary>
/// NextChat ModelConfig from app/store/config.ts.
/// </summary>
public sealed record ModelConfig(
    string Model,
    string ProviderName,
    double Temperature,
    double TopP,
    int MaxTokens,
    double PresencePenalty,
    double FrequencyPenalty,
    bool SendMemory,
    int HistoryMessageCount,
    int CompressMessageLengthThreshold,
    string CompressModel,
    string CompressProviderName,
    bool EnableInjectSystemPrompts,
    string Template,
    string Size,
    string Quality,
    string Style,
    bool Stream = true,
    ImmutableDictionary<string, JsonElement>? Extra = null)
{
    /// <summary>
    /// Default NextChat config from app/store/config.ts.
    /// </summary>
    public static ModelConfig NextChatDefault { get; } = new ModelConfig(
        "gpt-4o-mini",
        "OpenAI",
        0.5,
        1,
        4000,
        0,
        0,
        true,
        4,
        1000,
        string.Empty,
        string.Empty,
        true,
        "{{input}}",
        "1024x1024",
        "standard",
        "vivid");
}
