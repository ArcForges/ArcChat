// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;

namespace ArcChat.Protocol.Plugins;

/// <summary>
/// NextChat Plugin from app/store/plugin.ts.
/// </summary>
public sealed record Plugin(
    string Id,
    long CreatedAt,
    string Title,
    string Version,
    string Content,
    bool Builtin,
    string? AuthType = null,
    string? AuthLocation = null,
    string? AuthHeader = null,
    string? AuthTokenRef = null,
    PluginManifest? Manifest = null);

/// <summary>
/// OpenAPI manifest details extracted from a NextChat plugin schema.
/// </summary>
public sealed record PluginManifest(
    string Title,
    string Version,
    Uri? ServerUrl,
    ImmutableArray<PluginOperation> Operations);

/// <summary>
/// OpenAPI operation projected to a tool declaration.
/// </summary>
public sealed record PluginOperation(
    string OperationId,
    string Path,
    string Method,
    string? Description,
    JsonElement Parameters);
