// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Chat-only provider capabilities mapped from NextChat provider feature checks.
/// </summary>
[Flags]
public enum ChatProviderCapabilities
{
    /// <summary>
    /// No chat capability is declared.
    /// </summary>
    None = 0,

    /// <summary>
    /// Provider supports incremental chat output.
    /// </summary>
    Streaming = 1 << 0,

    /// <summary>
    /// Provider supports function or tool calls in chat.
    /// </summary>
    Tools = 1 << 1,

    /// <summary>
    /// Provider supports image content blocks in chat input.
    /// </summary>
    Vision = 1 << 2,

    /// <summary>
    /// Provider supports structured JSON chat output controls.
    /// </summary>
    JsonMode = 1 << 3,

    /// <summary>
    /// Provider can stream or return reasoning content.
    /// </summary>
    Reasoning = 1 << 4,
}
