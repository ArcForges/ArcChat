// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.Agent;

/// <summary>
/// Collapses older conversation messages into a bounded memory prompt.
/// </summary>
public interface IContextSummarizer
{
    /// <summary>
    /// Summarizes a conversation when the active model context window is exceeded.
    /// </summary>
    /// <param name="conversation">Conversation to summarize.</param>
    /// <param name="model">Active model descriptor with context-window metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The original conversation or a copy with updated memory fields.</returns>
    Task<Conversation> SummarizeAsync(Conversation conversation, ModelDescriptor model, CancellationToken cancellationToken = default);
}
