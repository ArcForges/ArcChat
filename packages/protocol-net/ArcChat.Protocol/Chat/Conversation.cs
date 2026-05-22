// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Masks;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat ChatSession mapped from app/store/chat.ts.
/// </summary>
/// <param name="Id">Stable conversation id.</param>
/// <param name="Topic">Conversation topic.</param>
/// <param name="MemoryPrompt">Summarized memory prompt.</param>
/// <param name="Messages">Conversation messages.</param>
/// <param name="Stat">Conversation statistics.</param>
/// <param name="LastUpdate">Last update Unix epoch milliseconds.</param>
/// <param name="LastSummarizeIndex">Last summarized message index.</param>
/// <param name="ClearContextIndex">Optional context-clear boundary.</param>
/// <param name="Mask">Conversation mask.</param>
public sealed record Conversation(
    string Id,
    string Topic,
    string MemoryPrompt,
    ImmutableArray<Message> Messages,
    ChatStat Stat,
    long LastUpdate,
    int LastSummarizeIndex,
    int? ClearContextIndex,
    Mask Mask);
