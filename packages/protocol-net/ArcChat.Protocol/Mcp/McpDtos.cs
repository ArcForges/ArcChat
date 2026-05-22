// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.Protocol.Mcp;

/// <summary>
/// JSON-RPC request shape from app/mcp/types.ts.
/// </summary>
public sealed record McpRequestMessage(
    string Method,
    string? JsonRpc = "2.0",
    JsonElement? Id = null,
    ImmutableDictionary<string, JsonElement>? Params = null);

/// <summary>
/// JSON-RPC response shape from app/mcp/types.ts.
/// </summary>
public sealed record McpResponseMessage(
    string? JsonRpc = "2.0",
    JsonElement? Id = null,
    ImmutableDictionary<string, JsonElement>? Result = null,
    McpError? Error = null);

/// <summary>
/// MCP JSON-RPC error payload.
/// </summary>
public sealed record McpError(int Code, string Message, JsonElement? Data = null);

/// <summary>
/// NextChat MCP server status from app/mcp/types.ts.
/// </summary>
public enum McpServerStatus
{
    /// <summary>
    /// Server status is not known.
    /// </summary>
    [JsonStringEnumMemberName("undefined")]
    Undefined,

    /// <summary>
    /// Server is active.
    /// </summary>
    [JsonStringEnumMemberName("active")]
    Active,

    /// <summary>
    /// Server is paused.
    /// </summary>
    [JsonStringEnumMemberName("paused")]
    Paused,

    /// <summary>
    /// Server is in error state.
    /// </summary>
    [JsonStringEnumMemberName("error")]
    Error,

    /// <summary>
    /// Server is initializing.
    /// </summary>
    [JsonStringEnumMemberName("initializing")]
    Initializing,
}

/// <summary>
/// NextChat ServerConfig from app/mcp/types.ts and mcp_config.default.json.
/// </summary>
public sealed record McpServerConfig(
    string Command,
    ImmutableArray<string> Args,
    ImmutableDictionary<string, string>? Env = null,
    string? Cwd = null,
    McpServerStatus Status = McpServerStatus.Undefined);

/// <summary>
/// NextChat MCP config file root.
/// </summary>
public sealed record McpConfigData(ImmutableDictionary<string, McpServerConfig> McpServers);

/// <summary>
/// MCP tool descriptor from list tools response.
/// </summary>
public sealed record McpTool(
    string Name,
    string? Description,
    JsonElement InputSchema,
    ImmutableDictionary<string, JsonElement>? Extra = null);
