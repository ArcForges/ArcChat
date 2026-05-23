// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using ArcChat.Protocol.Providers;
using ArcChat.Protocol.Settings;

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// NextChat-compatible settings defaults from app/store/config.ts.
/// </summary>
public static class SettingsDefaults
{
    /// <summary>
    /// Creates a default settings snapshot.
    /// </summary>
    public static SettingsSnapshot Create()
    {
        return new SettingsSnapshot(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            new UiSettings("Enter", "1f603", 14, string.Empty, "auto", true, true, 300),
            new ConversationSettings(true, true, true, false, false, false),
            new ProviderSettings(
                string.Empty,
                "gpt-4o-mini",
                ImmutableArray.Create(
                    CreateProviderConfig("openai", "OpenAI", "https://api.openai.com", "gpt-4o-mini", supportsVision: true),
                    CreateProviderConfig("anthropic", "Anthropic", "https://api.anthropic.com", "claude-3-5-sonnet-latest", supportsVision: true),
                    CreateProviderConfig("google", "Google", "https://generativelanguage.googleapis.com", "gemini-2.5-pro", supportsVision: true),
                    CreateGenericOpenAiProviderConfig())),
            ModelConfig.NextChatDefault,
            new TtsSettings(false, false, "OpenAI-TTS", "tts-1", "alloy", 1),
            new RealtimeSettings(
                false,
                "OpenAI",
                "gpt-4o-realtime-preview-2024-10-01",
                string.Empty,
                new AzureRealtimeSettings(string.Empty, string.Empty),
                0.9,
                "alloy"),
            new ShortcutSettings(ImmutableDictionary<string, string>.Empty));
    }

    private static ProviderConfig CreateProviderConfig(
        string id,
        string providerName,
        string baseUrl,
        string modelId,
        bool supportsVision)
    {
        return new ProviderConfig(
            id,
            providerName,
            new Uri(baseUrl),
            null,
            ImmutableArray.Create(CreateModelDescriptor(id, modelId, supportsVision)),
            ImmutableDictionary<string, JsonElement>.Empty);
    }

    private static ProviderConfig CreateGenericOpenAiProviderConfig()
    {
        return new ProviderConfig(
            "custom-openai",
            "GenericOpenAI",
            new Uri("http://localhost:8000"),
            null,
            ImmutableArray.Create(CreateModelDescriptor("custom-openai", "local-model", supportsVision: false)),
            ImmutableDictionary<string, JsonElement>.Empty);
    }

    private static ModelDescriptor CreateModelDescriptor(string providerId, string modelId, bool supportsVision)
    {
        ImmutableArray<ProviderCapability>.Builder capabilities = ImmutableArray.CreateBuilder<ProviderCapability>();
        capabilities.Add(new StreamingCapability());
        capabilities.Add(new ToolsCapability());
        if (supportsVision)
        {
            capabilities.Add(new VisionCapability());
        }

        return new ModelDescriptor(modelId, modelId, providerId, true, 0, capabilities.ToImmutable());
    }
}
