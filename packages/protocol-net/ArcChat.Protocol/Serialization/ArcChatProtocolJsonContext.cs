// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json.Serialization;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Mcp;
using ArcChat.Protocol.Plugins;
using ArcChat.Protocol.Providers;
using ArcChat.Protocol.Settings;

namespace ArcChat.Protocol.Serialization;

/// <summary>
/// Source-generated JSON metadata for ArcChat protocol DTOs.
/// </summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(Conversation))]
[JsonSerializable(typeof(ConversationExportDto))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(ContentBlock))]
[JsonSerializable(typeof(ChatEvent))]
[JsonSerializable(typeof(Mask))]
[JsonSerializable(typeof(Plugin))]
[JsonSerializable(typeof(PluginManifest))]
[JsonSerializable(typeof(McpConfigData))]
[JsonSerializable(typeof(McpRequestMessage))]
[JsonSerializable(typeof(McpResponseMessage))]
[JsonSerializable(typeof(McpTool))]
[JsonSerializable(typeof(HtmlArtifactPreview))]
[JsonSerializable(typeof(ArcTool))]
[JsonSerializable(typeof(ModelConfig))]
[JsonSerializable(typeof(ProviderConfig))]
[JsonSerializable(typeof(ModelDescriptor))]
[JsonSerializable(typeof(ProviderCapability))]
[JsonSerializable(typeof(SettingsSnapshot))]
[JsonSerializable(typeof(SyncSnapshot))]
[JsonSerializable(typeof(SyncProviderConfig))]
public partial class ArcChatProtocolJsonContext : JsonSerializerContext
{
}
