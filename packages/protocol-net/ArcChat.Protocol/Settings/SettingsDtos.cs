// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcChat.Protocol.Providers;

namespace ArcChat.Protocol.Settings;

/// <summary>
/// NextChat Config store snapshot from app/store/config.ts.
/// </summary>
public sealed record SettingsSnapshot(
    long LastUpdate,
    UiSettings Ui,
    ConversationSettings Conversation,
    ProviderSettings Providers,
    ModelConfig ModelConfig,
    TtsSettings Tts,
    RealtimeSettings Realtime,
    ImmutableDictionary<string, JsonElement>? Extra = null);

/// <summary>
/// NextChat UI settings from app/store/config.ts.
/// </summary>
public sealed record UiSettings(
    string SubmitKey,
    string Avatar,
    int FontSize,
    string FontFamily,
    string Theme,
    bool TightBorder,
    bool SendPreviewBubble,
    double SidebarWidth);

/// <summary>
/// NextChat conversation settings from app/store/config.ts.
/// </summary>
public sealed record ConversationSettings(
    bool EnableAutoGenerateTitle,
    bool EnableArtifacts,
    bool EnableCodeFold,
    bool DisablePromptHint,
    bool DontShowMaskSplashScreen,
    bool HideBuiltinMasks);

/// <summary>
/// Aggregated provider settings and custom model fields.
/// </summary>
public sealed record ProviderSettings(
    string CustomModels,
    string DefaultModel,
    ImmutableArray<ProviderConfig> ProviderConfigs);

/// <summary>
/// NextChat TTS settings from app/store/config.ts.
/// </summary>
public sealed record TtsSettings(bool Enable, bool Autoplay, string Engine, string Model, string Voice, double Speed);

/// <summary>
/// NextChat realtime settings from app/store/config.ts.
/// </summary>
public sealed record RealtimeSettings(
    bool Enable,
    string Provider,
    string Model,
    string? ApiKeyRef,
    AzureRealtimeSettings Azure,
    double Temperature,
    string Voice);

/// <summary>
/// Azure realtime settings from NextChat realtimeConfig.azure.
/// </summary>
public sealed record AzureRealtimeSettings(string Endpoint, string Deployment);

/// <summary>
/// NextChat full app-state sync snapshot from app/utils/sync.ts.
/// </summary>
public sealed record SyncSnapshot(
    string SchemaVersion,
    DateTimeOffset ExportedAt,
    ImmutableDictionary<string, JsonElement> Stores);

/// <summary>
/// Sync provider config base from app/store/sync.ts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WebDavSyncProviderConfig), "webdav")]
[JsonDerivedType(typeof(UpstashSyncProviderConfig), "upstash")]
public abstract record SyncProviderConfig(string Provider, bool UseProxy, Uri? ProxyUrl);

/// <summary>
/// WebDAV sync provider config from app/store/sync.ts.
/// </summary>
public sealed record WebDavSyncProviderConfig(
    Uri Endpoint,
    string Username,
    string PasswordRef,
    bool UseProxy = false,
    Uri? ProxyUrl = null)
    : SyncProviderConfig("webdav", UseProxy, ProxyUrl);

/// <summary>
/// Upstash sync provider config from app/store/sync.ts.
/// </summary>
public sealed record UpstashSyncProviderConfig(
    Uri Endpoint,
    string BackupName,
    string ApiKeyRef,
    bool UseProxy = false,
    Uri? ProxyUrl = null)
    : SyncProviderConfig("upstash", UseProxy, ProxyUrl);
