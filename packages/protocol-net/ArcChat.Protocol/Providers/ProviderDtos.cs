// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.Protocol.Providers;

/// <summary>
/// Per-provider settings mapped from NextChat access/config stores.
/// </summary>
public sealed record ProviderConfig(
    string Id,
    string ProviderName,
    Uri? BaseUrl,
    string? ApiKeyRef,
    ImmutableArray<ModelDescriptor> Models,
    ImmutableDictionary<string, JsonElement>? Extra = null);

/// <summary>
/// NextChat LLMModel mapped from app/client/api.ts.
/// </summary>
public sealed record ModelDescriptor(
    string Id,
    string DisplayName,
    string ProviderId,
    bool Available,
    int Sorted,
    ImmutableArray<ProviderCapability> Capabilities,
    int? ContextWindow = null,
    ImmutableDictionary<string, JsonElement>? Extra = null);

/// <summary>
/// Provider capability marker. Image, TTS, and realtime use separate SPIs.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StreamingCapability), "streaming")]
[JsonDerivedType(typeof(ToolsCapability), "tools")]
[JsonDerivedType(typeof(VisionCapability), "vision")]
[JsonDerivedType(typeof(JsonModeCapability), "json-mode")]
[JsonDerivedType(typeof(ReasoningCapability), "reasoning")]
public abstract record ProviderCapability;

/// <summary>
/// Chat streaming capability.
/// </summary>
public sealed record StreamingCapability : ProviderCapability;

/// <summary>
/// Chat tool-call capability.
/// </summary>
public sealed record ToolsCapability : ProviderCapability;

/// <summary>
/// Chat vision input capability.
/// </summary>
public sealed record VisionCapability : ProviderCapability;

/// <summary>
/// JSON mode capability.
/// </summary>
public sealed record JsonModeCapability : ProviderCapability;

/// <summary>
/// Reasoning-content stream capability.
/// </summary>
public sealed record ReasoningCapability : ProviderCapability;
