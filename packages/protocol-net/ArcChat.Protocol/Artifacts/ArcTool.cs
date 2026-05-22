// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;

namespace ArcChat.Protocol.Artifacts;

/// <summary>
/// ArcChat tool DTO for OpenAPI plugins and MCP tool calls.
/// </summary>
public sealed record ArcTool(
    string Id,
    string Name,
    string Description,
    JsonElement InputSchema,
    JsonElement OutputSchema,
    ToolPermissionKind Permission);
