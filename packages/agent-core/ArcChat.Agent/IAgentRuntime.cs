// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.Agent;

/// <summary>
/// ArcChat-owned runtime facade for streaming assistant responses.
/// </summary>
public interface IAgentRuntime
{
    /// <summary>
    /// Streams ordered chat events for an agent request.
    /// </summary>
    /// <param name="request">Agent request.</param>
    /// <param name="cancellationToken">Cancellation token used for user abort.</param>
    /// <returns>An ordered stream of chat events.</returns>
    IAsyncEnumerable<ChatEvent> StreamAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
