// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.Agent.Tests;

public sealed class AgentRuntimeTests
{
    [Fact]
    public async Task StreamAsyncPreservesProviderEventOrder()
    {
        SequenceProvider provider = new SequenceProvider(
            new MessageDelta("c1", "m1", "hel"),
            new MessageDelta("c1", "m1", "lo"),
            new MessageCompleted("c1", "m1", Assistant("m1", "hello")),
            new ChatFinished("c1", "m1", "stop"));
        AgentRuntime runtime = CreateRuntime(provider);

        IReadOnlyList<ChatEvent> events = await CollectAsync(runtime.StreamAsync(CreateRequest()));

        _ = events.Select(static chatEvent => chatEvent.GetType()).Should().Equal(
            typeof(MessageDelta),
            typeof(MessageDelta),
            typeof(MessageCompleted),
            typeof(ChatFinished));
    }

    [Fact]
    public async Task StreamAsyncRunsNoOpToolLoop()
    {
        ChatMessageTool tool = new ChatMessageTool("tool-1", "noop", "{\"value\":1}");
        SequenceProvider provider = new SequenceProvider(new ToolCallStarted("c1", "m1", tool), new ChatFinished("c1", "m1", "stop"));
        AgentRuntime runtime = CreateRuntime(provider);

        IReadOnlyList<ChatEvent> events = await CollectAsync(runtime.StreamAsync(CreateRequest()));

        _ = events.Should().HaveCount(3);
        ToolCallCompleted completed = events.OfType<ToolCallCompleted>().Single();
        _ = completed.Tool.Id.Should().Be("tool-1");
        _ = completed.Result.GetRawText().Should().Be("{}");
    }

    [Fact]
    public async Task StreamAsyncRetriesTransientNetworkFailureOnce()
    {
        RetryProvider provider = new RetryProvider();
        AgentRuntime runtime = CreateRuntime(provider);

        IReadOnlyList<ChatEvent> events = await CollectAsync(runtime.StreamAsync(CreateRequest()));

        _ = provider.Attempts.Should().Be(2);
        _ = events.Should().ContainSingle(static chatEvent => chatEvent is MessageDelta);
        _ = events.Should().NotContain(static chatEvent => chatEvent is ChatError);
    }

    [Fact]
    public async Task StreamAsyncStopsWithinAbortBudget()
    {
        SlowProvider provider = new SlowProvider();
        AgentRuntime runtime = CreateRuntime(provider);
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        Stopwatch stopwatch = Stopwatch.StartNew();
        Task<IReadOnlyList<ChatEvent>> streamTask = CollectAsync(runtime.StreamAsync(CreateRequest(), cancellation.Token), cancellation.Token);
        await cancellation.CancelAsync().ConfigureAwait(true);

        Func<Task> act = async () => await streamTask.ConfigureAwait(true);
        _ = await act.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
        _ = stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    private static AgentRuntime CreateRuntime(IChatProvider provider)
    {
        ChatProviderRegistry registry = new ChatProviderRegistry();
        registry.Register(provider);
        return new AgentRuntime(registry);
    }

    private static AgentRequest CreateRequest()
    {
        return new AgentRequest(
            "c1",
            "m1",
            new[] { Message.Text("u1", MessageRole.User, "hello", "0") },
            ModelConfig.NextChatDefault);
    }

    private static Message Assistant(string id, string text)
    {
        return new Message(
            id,
            MessageRole.Assistant,
            ImmutableArray.Create<ContentBlock>(new TextBlock(text)),
            "0",
            Tools: ImmutableArray<ChatMessageTool>.Empty);
    }

    private static async Task<IReadOnlyList<ChatEvent>> CollectAsync(
        IAsyncEnumerable<ChatEvent> stream,
        CancellationToken cancellationToken = default)
    {
        List<ChatEvent> events = new List<ChatEvent>();
        await foreach (ChatEvent chatEvent in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            events.Add(chatEvent);
        }

        return events;
    }

    private sealed class SequenceProvider : IChatProvider
    {
        private readonly IReadOnlyList<ChatEvent> events;

        public SequenceProvider(params ChatEvent[] events)
        {
            this.events = events;
        }

        public string Id => "OpenAI";

        public bool SupportsVision => false;

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatProviderRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (ChatEvent chatEvent in this.events)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                yield return chatEvent;
            }
        }
    }

    private sealed class RetryProvider : IChatProvider
    {
        public int Attempts { get; private set; }

        public string Id => "OpenAI";

        public bool SupportsVision => false;

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatProviderRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.Attempts++;
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            if (this.Attempts == 1)
            {
                throw new IOException("temporary network fault");
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return new MessageDelta(request.ConversationId, request.MessageId, "ok");
            yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
        }
    }

    private sealed class SlowProvider : IChatProvider
    {
        public string Id => "OpenAI";

        public bool SupportsVision => false;

        public async IAsyncEnumerable<ChatEvent> StreamAsync(
            ChatProviderRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            yield return new MessageDelta(request.ConversationId, request.MessageId, "late");
        }
    }
}
