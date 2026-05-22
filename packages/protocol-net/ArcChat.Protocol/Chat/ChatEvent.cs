// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// Streaming chat event contract used by providers and agent runtime.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MessageDelta), "message-delta")]
[JsonDerivedType(typeof(MessageCompleted), "message-completed")]
[JsonDerivedType(typeof(ToolCallStarted), "tool-call-started")]
[JsonDerivedType(typeof(ToolCallCompleted), "tool-call-completed")]
[JsonDerivedType(typeof(ReasoningDelta), "reasoning-delta")]
[JsonDerivedType(typeof(ChatError), "error")]
[JsonDerivedType(typeof(ChatFinished), "finished")]
public abstract record ChatEvent(string ConversationId, string MessageId);

/// <summary>
/// Text delta emitted while a NextChat assistant message is streaming.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Delta">Delta text.</param>
public sealed record MessageDelta(string ConversationId, string MessageId, string Delta)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// Final assistant message content after streaming completes.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Message">Completed message.</param>
public sealed record MessageCompleted(string ConversationId, string MessageId, Message Message)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// Tool call start event mapped from NextChat onBeforeTool.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Tool">Tool metadata.</param>
public sealed record ToolCallStarted(string ConversationId, string MessageId, ChatMessageTool Tool)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// Tool call completion event mapped from NextChat onAfterTool.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Tool">Completed tool metadata.</param>
/// <param name="Result">Opaque tool result payload.</param>
public sealed record ToolCallCompleted(
    string ConversationId,
    string MessageId,
    ChatMessageTool Tool,
    JsonElement Result)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// Provider reasoning delta for OpenAI-compatible reasoning streams.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Delta">Reasoning text delta.</param>
public sealed record ReasoningDelta(string ConversationId, string MessageId, string Delta)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// Normalized chat streaming error.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="Code">Stable error code.</param>
/// <param name="Message">Human-readable error message.</param>
public sealed record ChatError(string ConversationId, string MessageId, string Code, string Message)
    : ChatEvent(ConversationId, MessageId);

/// <summary>
/// End-of-stream marker.
/// </summary>
/// <param name="ConversationId">Conversation id.</param>
/// <param name="MessageId">Message id.</param>
/// <param name="FinishReason">Provider finish reason.</param>
public sealed record ChatFinished(string ConversationId, string MessageId, string? FinishReason = null)
    : ChatEvent(ConversationId, MessageId);
