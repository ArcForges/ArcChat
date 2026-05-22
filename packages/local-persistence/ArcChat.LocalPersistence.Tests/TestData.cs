// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using ArcChat.Protocol.Settings;

namespace ArcChat.LocalPersistence.Tests;

internal static class TestData
{
    public static string CreateDatabasePath()
    {
        string directory = Path.Join(Path.GetTempPath(), "ArcChat.LocalPersistence.Tests");
        Directory.CreateDirectory(directory);
        return Path.Join(directory, Guid.NewGuid().ToString("N") + ".db");
    }

    public static Conversation CreateConversation(string id, int messageCount = 0)
    {
        ImmutableArray<Message> messages = Enumerable.Range(0, messageCount)
            .Select(index => CreateMessage("m-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), "message " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToImmutableArray();

        return new Conversation(
            id,
            "Topic " + id,
            string.Empty,
            messages,
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            CreateMask("mask-" + id));
    }

    public static Message CreateMessage(string id, string text)
    {
        return Message.Text(id, MessageRole.Assistant, text, "2026-05-22");
    }

    public static Mask CreateMask(string id)
    {
        return new Mask(
            id,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            "1f600",
            "Mask " + id,
            false,
            ImmutableArray<Message>.Empty,
            true,
            ModelConfig.NextChatDefault,
            "en",
            false,
            ImmutableArray<string>.Empty);
    }

    public static SettingsSnapshot CreateSettingsSnapshot(string apiKeyRef)
    {
        return new SettingsSnapshot(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            new UiSettings("Enter", "1f600", 14, "Inter", "auto", false, true, 320),
            new ConversationSettings(true, true, true, false, false, false),
            new ProviderSettings(
                string.Empty,
                "gpt-4o-mini",
                ImmutableArray.Create(new ProviderConfig("openai", "OpenAI", new Uri("https://api.openai.com"), apiKeyRef, ImmutableArray<ModelDescriptor>.Empty))),
            ModelConfig.NextChatDefault,
            new TtsSettings(false, false, "none", string.Empty, string.Empty, 1),
            new RealtimeSettings(false, "openai", "gpt-4o-realtime", apiKeyRef, new AzureRealtimeSettings(string.Empty, string.Empty), 0.5, "alloy"));
    }
}
