// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Provider-specific extension data plus ArcChat stream identity.
/// </summary>
public sealed record ProviderExtra
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderExtra"/> class.
    /// </summary>
    /// <param name="conversationId">Conversation id that owns emitted chat events.</param>
    /// <param name="messageId">Assistant message id that receives emitted chat events.</param>
    /// <param name="values">Opaque provider-specific request values.</param>
    public ProviderExtra(
        string conversationId,
        string messageId,
        ImmutableDictionary<string, JsonElement>? values = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        this.ConversationId = conversationId;
        this.MessageId = messageId;
        this.Values = values ?? ImmutableDictionary<string, JsonElement>.Empty;
    }

    /// <summary>
    /// Gets the conversation id that owns emitted chat events.
    /// </summary>
    public string ConversationId { get; init; }

    /// <summary>
    /// Gets the assistant message id that receives emitted chat events.
    /// </summary>
    public string MessageId { get; init; }

    /// <summary>
    /// Gets opaque provider-specific request values.
    /// </summary>
    public ImmutableDictionary<string, JsonElement> Values { get; init; }

    /// <summary>
    /// Creates request metadata with empty provider-specific values.
    /// </summary>
    /// <param name="conversationId">Conversation id that owns emitted chat events.</param>
    /// <param name="messageId">Assistant message id that receives emitted chat events.</param>
    /// <returns>Request metadata for a chat stream.</returns>
    public static ProviderExtra ForStream(string conversationId, string messageId)
    {
        return new ProviderExtra(conversationId, messageId, ImmutableDictionary<string, JsonElement>.Empty);
    }
}
