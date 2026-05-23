// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ArcChat.ModelProviders.Core.Tests;

public sealed class ChatProviderRegistryTests
{
    [Fact]
    public void ResolveUsesProviderIdAndModelConfig()
    {
        ChatProviderRegistry registry = new ChatProviderRegistry();
        TestProvider openAi = new TestProvider("OpenAI", ChatProviderCapabilities.Streaming | ChatProviderCapabilities.Tools);
        TestProvider anthropic = new TestProvider("Anthropic", ChatProviderCapabilities.Streaming);
        registry.Register(openAi);
        registry.Register(anthropic);

        IChatProvider resolved = registry.Resolve(new ProviderId("openai"), ModelConfig.NextChatDefault);

        _ = resolved.Should().BeSameAs(openAi);
        _ = registry.TryResolve(new ProviderId("Anthropic"), ModelConfig.NextChatDefault, out IChatProvider provider)
            .Should()
            .BeTrue();
        _ = provider.Should().BeSameAs(anthropic);
    }

    [Fact]
    public void AddArcChatProvidersRegistersRegistryWithCustomProviders()
    {
        TestProvider provider = new TestProvider("OpenAI", ChatProviderCapabilities.Streaming);
        ServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton<IChatProvider>(provider);
        _ = services.AddArcChatProviders();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IChatProviderRegistry registry = serviceProvider.GetRequiredService<IChatProviderRegistry>();

        _ = registry.Resolve(new ProviderId("OpenAI"), ModelConfig.NextChatDefault).Should().BeSameAs(provider);
    }

    [Fact]
    public async Task DecoratorChainRetriesTransientFailureAndStreamsResult()
    {
        FlakyProvider provider = new FlakyProvider();
        using TokenBucketRateLimiter limiter = CreateLimiter(tokenLimit: 4, queueLimit: 4, autoReplenishment: true);
        using RateLimitedChatProvider rateLimited = new RateLimitedChatProvider(provider, limiter);
        RetryingChatProvider retrying = new RetryingChatProvider(rateLimited);

        IReadOnlyList<ChatEvent> events = await CollectAsync(retrying.StreamAsync(CreateRequest("m1"))).ConfigureAwait(true);

        _ = provider.Attempts.Should().Be(2);
        _ = events.OfType<MessageDelta>().Single().Delta.Should().Be("ok");
        _ = events[^1].Should().BeOfType<ChatFinished>();
    }

    [Fact]
    public async Task RateLimitAwaitsQueuedStreamInsteadOfDroppingIt()
    {
        using TokenBucketRateLimiter limiter = CreateLimiter(tokenLimit: 1, queueLimit: 1, autoReplenishment: false);
        using RateLimitedChatProvider provider = new RateLimitedChatProvider(new TestProvider("OpenAI", ChatProviderCapabilities.Streaming), limiter);

        Task<IReadOnlyList<ChatEvent>> first = CollectAsync(provider.StreamAsync(CreateRequest("m1")));
        Task<IReadOnlyList<ChatEvent>> second = CollectAsync(provider.StreamAsync(CreateRequest("m2")));

        _ = (await first.ConfigureAwait(true)).Should().ContainSingle(static chatEvent => chatEvent is MessageDelta);
        await WaitForQueuedLeaseAsync(limiter).ConfigureAwait(true);
        _ = second.IsCompleted.Should().BeFalse();

        await Task.Delay(TimeSpan.FromMilliseconds(25)).ConfigureAwait(true);
        _ = limiter.TryReplenish().Should().BeTrue();
        IReadOnlyList<ChatEvent> secondEvents = await second.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(true);

        _ = secondEvents.Should().ContainSingle(static chatEvent => chatEvent is MessageDelta);
    }

    private static TokenBucketRateLimiter CreateLimiter(int tokenLimit, int queueLimit, bool autoReplenishment)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = autoReplenishment,
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(20),
            TokenLimit = tokenLimit,
            TokensPerPeriod = tokenLimit,
        });
    }

    private static async Task WaitForQueuedLeaseAsync(TokenBucketRateLimiter limiter)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(1);
        while (limiter.GetStatistics()?.CurrentQueuedCount != 1)
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Expected the rate limiter to queue the second stream.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(true);
        }
    }

    private static ChatRequest CreateRequest(string messageId)
    {
        return new ChatRequest(
            ImmutableArray.Create(Message.Text("u1", MessageRole.User, "hello", "0")),
            ModelConfig.NextChatDefault,
            ImmutableArray<ArcTool>.Empty,
            ProviderExtra.ForStream("c1", messageId));
    }

    private static async Task<IReadOnlyList<ChatEvent>> CollectAsync(IAsyncEnumerable<ChatEvent> stream)
    {
        List<ChatEvent> events = new List<ChatEvent>();
        await foreach (ChatEvent chatEvent in stream.ConfigureAwait(false))
        {
            events.Add(chatEvent);
        }

        return events;
    }

    private sealed class TestProvider : IChatProvider
    {
        public TestProvider(string id, ChatProviderCapabilities capabilities)
        {
            this.Id = new ProviderId(id);
            this.Capabilities = capabilities;
        }

        public ProviderId Id { get; }

        public ChatProviderCapabilities Capabilities { get; }

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new MessageDelta(request.Extra.ConversationId, request.Extra.MessageId, "ok");
        }

        public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ImmutableArray<ModelDescriptor>.Empty);
        }
    }

    private sealed class FlakyProvider : IChatProvider
    {
        public int Attempts { get; private set; }

        public ProviderId Id => new ProviderId("OpenAI");

        public ChatProviderCapabilities Capabilities => ChatProviderCapabilities.Streaming;

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.Attempts++;
            await Task.Yield();
            if (this.Attempts == 1)
            {
                throw new IOException("temporary provider fault");
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return new MessageDelta(request.Extra.ConversationId, request.Extra.MessageId, "ok");
            yield return new ChatFinished(request.Extra.ConversationId, request.Extra.MessageId, "stop");
        }

        public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ImmutableArray<ModelDescriptor>.Empty);
        }
    }
}
