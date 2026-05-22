// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.Agent;

/// <summary>
/// Provider-neutral request consumed by the ArcChat agent runtime.
/// </summary>
public sealed record AgentRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRequest"/> class.
    /// </summary>
    /// <param name="conversationId">Conversation id that owns the stream.</param>
    /// <param name="messageId">Assistant message id receiving streamed events.</param>
    /// <param name="messages">Ordered NextChat-compatible messages used as model context.</param>
    /// <param name="model">Selected model configuration.</param>
    /// <param name="maxTransientRetries">Maximum retry count for transient network failures.</param>
    /// <param name="transientRetryDelay">Delay between transient retry attempts.</param>
    public AgentRequest(
        string conversationId,
        string messageId,
        IReadOnlyList<Message> messages,
        ModelConfig model,
        int maxTransientRetries = 1,
        TimeSpan transientRetryDelay = default)
    {
        this.ConversationId = conversationId;
        this.MessageId = messageId;
        this.Messages = messages;
        this.Model = model;
        this.MaxTransientRetries = maxTransientRetries;
        this.TransientRetryDelay = transientRetryDelay;
    }

    /// <summary>
    /// Gets the conversation id that owns the stream.
    /// </summary>
    public string ConversationId { get; init; }

    /// <summary>
    /// Gets the assistant message id receiving streamed events.
    /// </summary>
    public string MessageId { get; init; }

    /// <summary>
    /// Gets the ordered NextChat-compatible messages used as model context.
    /// </summary>
    public IReadOnlyList<Message> Messages { get; init; }

    /// <summary>
    /// Gets the selected model configuration.
    /// </summary>
    public ModelConfig Model { get; init; }

    /// <summary>
    /// Gets the maximum retry count for transient network failures.
    /// </summary>
    public int MaxTransientRetries { get; init; }

    /// <summary>
    /// Gets the delay between transient retry attempts.
    /// </summary>
    public TimeSpan TransientRetryDelay { get; init; }
}
