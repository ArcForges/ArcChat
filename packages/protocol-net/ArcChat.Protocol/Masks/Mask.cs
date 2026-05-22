// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.Protocol.Masks;

/// <summary>
/// NextChat Mask from app/store/mask.ts.
/// </summary>
public sealed record Mask(
    string Id,
    long CreatedAt,
    string Avatar,
    string Name,
    bool? HideContext,
    ImmutableArray<Message> Context,
    bool SyncGlobalConfig,
    ModelConfig ModelConfig,
    string Lang,
    bool Builtin,
    ImmutableArray<string> Plugin = default,
    bool? EnableArtifacts = null,
    bool? EnableCodeFold = null);
