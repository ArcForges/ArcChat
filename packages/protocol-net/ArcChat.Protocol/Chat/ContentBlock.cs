// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat multimodal message content from app/client/api.ts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(ToolCallBlock), "tool-call")]
[JsonDerivedType(typeof(ToolResultBlock), "tool-result")]
public abstract record ContentBlock;

/// <summary>
/// Text content block mapped from NextChat string content.
/// </summary>
/// <param name="Text">Text payload.</param>
public sealed record TextBlock(string Text) : ContentBlock;

/// <summary>
/// Image URL content block mapped from NextChat multimodal image_url parts.
/// </summary>
/// <param name="Url">Image URL or data URL.</param>
/// <param name="Detail">Optional provider-specific detail level.</param>
public sealed record ImageBlock(string Url, string? Detail = null) : ContentBlock;

/// <summary>
/// Tool call content block mapped from NextChat ChatMessageTool.
/// </summary>
/// <param name="Id">Provider tool call id.</param>
/// <param name="Name">Tool or function name.</param>
/// <param name="Arguments">Tool input arguments.</param>
/// <param name="Index">Provider stream index.</param>
/// <param name="ToolType">Provider tool type.</param>
public sealed record ToolCallBlock(
    string Id,
    string Name,
    JsonElement Arguments,
    int? Index = null,
    string? ToolType = null) : ContentBlock;

/// <summary>
/// Tool result content block mapped from NextChat tool response content.
/// </summary>
/// <param name="ToolCallId">Tool call id that produced the result.</param>
/// <param name="Name">Tool or function name.</param>
/// <param name="Content">Result content.</param>
/// <param name="IsError">Whether the tool result represents a failure.</param>
/// <param name="Extra">Opaque provider or MCP result fields.</param>
public sealed record ToolResultBlock(
    string ToolCallId,
    string Name,
    string Content,
    bool IsError = false,
    ImmutableDictionary<string, JsonElement>? Extra = null) : ContentBlock;
