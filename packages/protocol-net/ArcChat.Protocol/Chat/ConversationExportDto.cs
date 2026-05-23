// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// Portable conversation export payload used by desktop exporters and share links.
/// </summary>
/// <param name="ConversationId">Stable conversation id.</param>
/// <param name="Topic">Conversation topic at export time.</param>
/// <param name="MemoryPrompt">Summarized memory prompt at export time.</param>
/// <param name="Messages">Selected messages included in the export.</param>
/// <param name="ExportedAt">Export timestamp.</param>
/// <param name="ClearContextIndex">Optional NextChat context-clear boundary.</param>
public sealed record ConversationExportDto(
    string ConversationId,
    string Topic,
    string MemoryPrompt,
    ImmutableArray<Message> Messages,
    DateTimeOffset ExportedAt,
    int? ClearContextIndex = null);
