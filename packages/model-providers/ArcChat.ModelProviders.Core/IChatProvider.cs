// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Streams provider-native model output as ArcChat protocol events.
/// </summary>
public interface IChatProvider
{
    /// <summary>
    /// Gets the stable provider id, for example <c>OpenAI</c> or <c>Echo</c>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets a value indicating whether the provider can accept image content blocks.
    /// </summary>
    bool SupportsVision { get; }

    /// <summary>
    /// Streams chat events for the supplied request.
    /// </summary>
    /// <param name="request">Provider-neutral request payload.</param>
    /// <param name="cancellationToken">Cancellation token used for user abort.</param>
    /// <returns>An ordered stream of chat events.</returns>
    IAsyncEnumerable<ChatEvent> StreamAsync(ChatProviderRequest request, CancellationToken cancellationToken = default);
}
