// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using Polly;
using Polly.Retry;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Chat provider decorator that retries transient provider streams with Polly.
/// </summary>
public sealed class RetryingChatProvider : IChatProvider
{
    private readonly IChatProvider inner;
    private readonly ResiliencePipeline pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryingChatProvider"/> class.
    /// </summary>
    /// <param name="inner">Provider to decorate.</param>
    /// <param name="pipeline">Optional Polly resilience pipeline.</param>
    public RetryingChatProvider(IChatProvider inner, ResiliencePipeline? pipeline = null)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.pipeline = pipeline ?? CreateDefaultPipeline();
    }

    /// <inheritdoc />
    public ProviderId Id => this.inner.Id;

    /// <inheritdoc />
    public ChatProviderCapabilities Capabilities => this.inner.Capabilities;

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatEvent> events = await this.pipeline.ExecuteAsync(
            async token =>
            {
                List<ChatEvent> attemptEvents = new List<ChatEvent>();
                await foreach (ChatEvent chatEvent in this.inner.StreamAsync(request, token).ConfigureAwait(false))
                {
                    attemptEvents.Add(chatEvent);
                }

                return attemptEvents;
            },
            cancellationToken).ConfigureAwait(false);

        foreach (ChatEvent chatEvent in events)
        {
            yield return chatEvent;
        }
    }

    /// <inheritdoc />
    public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return this.inner.ListModelsAsync(cancellationToken);
    }

    private static ResiliencePipeline CreateDefaultPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(100),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
                    .Handle<TimeoutException>(),
            })
            .Build();
    }
}
