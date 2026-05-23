// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Streams provider-native model output as ArcChat protocol events.
/// </summary>
public interface IChatProvider
{
    /// <summary>
    /// Gets the stable provider id, for example <c>OpenAI</c> or <c>Echo</c>.
    /// </summary>
    ProviderId Id { get; }

    /// <summary>
    /// Gets the chat capabilities exposed by this provider.
    /// </summary>
    ChatProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Streams chat events for the supplied request.
    /// </summary>
    /// <param name="request">Provider-neutral request payload.</param>
    /// <param name="cancellationToken">Cancellation token used for user abort.</param>
    /// <returns>An ordered stream of chat events.</returns>
    IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists provider-owned chat models that can be selected in ArcChat.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used for refresh abort.</param>
    /// <returns>The available chat model descriptors.</returns>
    Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default);
}
