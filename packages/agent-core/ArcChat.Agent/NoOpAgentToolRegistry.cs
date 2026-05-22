// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;

namespace ArcChat.Agent;

/// <summary>
/// NC04 placeholder tool registry that acknowledges tool calls without side effects.
/// </summary>
public sealed class NoOpAgentToolRegistry : IAgentToolRegistry
{
    private NoOpAgentToolRegistry()
    {
    }

    /// <summary>
    /// Gets the singleton no-op registry.
    /// </summary>
    public static NoOpAgentToolRegistry Instance { get; } = new NoOpAgentToolRegistry();

    /// <inheritdoc />
    public ValueTask<AgentToolResult> ExecuteAsync(
        AgentToolRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        using JsonDocument document = JsonDocument.Parse("{}");
        return ValueTask.FromResult(new AgentToolResult(request.Id, request.Name, document.RootElement.Clone()));
    }
}
