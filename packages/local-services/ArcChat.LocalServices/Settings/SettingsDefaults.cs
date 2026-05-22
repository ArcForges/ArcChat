// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
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
                    new ProviderConfig(
                        "openai",
                        "OpenAI",
                        new Uri("https://api.openai.com"),
                        null,
                        ImmutableArray<ModelDescriptor>.Empty))),
            ModelConfig.NextChatDefault,
            new TtsSettings(false, false, "OpenAI-TTS", "tts-1", "alloy", 1),
            new RealtimeSettings(
                false,
                "OpenAI",
                "gpt-4o-realtime-preview-2024-10-01",
                string.Empty,
                new AzureRealtimeSettings(string.Empty, string.Empty),
                0.9,
                "alloy"));
    }
}
