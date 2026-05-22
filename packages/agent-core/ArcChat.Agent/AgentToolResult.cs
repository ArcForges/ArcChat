// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;

namespace ArcChat.Agent;

/// <summary>
/// Tool invocation result returned to the agent stream.
/// </summary>
public sealed record AgentToolResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentToolResult"/> class.
    /// </summary>
    /// <param name="id">Provider tool call id.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="result">Opaque result payload.</param>
    /// <param name="isError">Whether the tool failed.</param>
    /// <param name="errorMessage">Optional error message.</param>
    public AgentToolResult(
        string id,
        string name,
        JsonElement result,
        bool isError = false,
        string? errorMessage = null)
    {
        this.Id = id;
        this.Name = name;
        this.Result = result;
        this.IsError = isError;
        this.ErrorMessage = errorMessage;
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
    /// Gets the opaque result payload.
    /// </summary>
    public JsonElement Result { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tool failed.
    /// </summary>
    public bool IsError { get; init; }

    /// <summary>
    /// Gets the optional error message.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
