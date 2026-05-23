// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Tokenizer;

/// <summary>
/// Counts prompt tokens for provider model descriptors.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Counts prompt tokens for the supplied messages and model descriptor.
    /// </summary>
    /// <param name="messages">Messages to count.</param>
    /// <param name="model">Model descriptor that selects the tokenizer strategy.</param>
    /// <returns>Estimated prompt token count.</returns>
    int Count(IEnumerable<Message> messages, ModelDescriptor model);
}
