// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Agent;

/// <summary>
/// Executes provider tool-call requests for the agent runtime.
/// </summary>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Executes one tool request.
    /// </summary>
    /// <param name="request">Tool request.</param>
    /// <param name="cancellationToken">Cancellation token used for user abort.</param>
    /// <returns>Tool execution result.</returns>
    ValueTask<AgentToolResult> ExecuteAsync(AgentToolRequest request, CancellationToken cancellationToken = default);
}
