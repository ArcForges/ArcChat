// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;

namespace ArcChat.Agent;

/// <summary>
/// Tool invocation request produced from a provider tool-call event.
/// </summary>
public sealed record AgentToolRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentToolRequest"/> class.
    /// </summary>
    /// <param name="id">Provider tool call id.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="arguments">Tool arguments as provider JSON.</param>
    public AgentToolRequest(string id, string name, JsonElement arguments)
    {
        this.Id = id;
        this.Name = name;
        this.Arguments = arguments;
    }

    /// <summary>
    /// Gets the provider tool call id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the tool arguments as provider JSON.
    /// </summary>
    public JsonElement Arguments { get; init; }
}
