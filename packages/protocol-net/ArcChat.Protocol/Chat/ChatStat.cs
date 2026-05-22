// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat ChatStat from app/store/chat.ts.
/// </summary>
/// <param name="TokenCount">Estimated token count.</param>
/// <param name="WordCount">Word count.</param>
/// <param name="CharCount">Character count.</param>
public sealed record ChatStat(int TokenCount, int WordCount, int CharCount);
