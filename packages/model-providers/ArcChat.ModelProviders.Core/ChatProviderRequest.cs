// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Provider-neutral request payload for a single assistant stream.
/// </summary>
public sealed record ChatProviderRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatProviderRequest"/> class.
    /// </summary>
    /// <param name="conversationId">Conversation id that owns the stream.</param>
    /// <param name="messageId">Assistant message id receiving streamed events.</param>
    /// <param name="messages">Ordered NextChat-compatible conversation messages.</param>
    /// <param name="model">Model configuration selected for the request.</param>
    public ChatProviderRequest(
        string conversationId,
        string messageId,
        IReadOnlyList<Message> messages,
        ModelConfig model)
    {
        this.ConversationId = conversationId;
        this.MessageId = messageId;
        this.Messages = messages;
        this.Model = model;
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
    /// Gets the ordered NextChat-compatible conversation messages.
    /// </summary>
    public IReadOnlyList<Message> Messages { get; init; }

    /// <summary>
    /// Gets the model configuration selected for the request.
    /// </summary>
    public ModelConfig Model { get; init; }
}
