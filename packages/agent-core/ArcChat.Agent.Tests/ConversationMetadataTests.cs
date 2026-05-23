// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.Agent.Tests;

public sealed class ConversationMetadataTests
{
    [Fact]
    public async Task ConversationTitlerUsesProviderOutput()
    {
        ScriptedProvider provider = new ScriptedProvider(CompletedText("\"Neural roadmap!\""));
        ChatProviderRegistry registry = new ChatProviderRegistry();
        registry.Register(provider);
        ConversationTitler titler = new ConversationTitler(registry);
        Conversation conversation = CreateConversation(
            ConversationTitler.DefaultTopic,
            Message.Text("u1", MessageRole.User, "Can you outline a neural roadmap?", "0"),
            Message.Text("a1", MessageRole.Assistant, "Sure.", "0"));

        string title = await titler.GenerateTitleAsync(conversation, CancellationToken.None).ConfigureAwait(true);

        _ = title.Should().Be("Neural roadmap");
        ChatProviderRequest request = provider.Requests.Should().ContainSingle().Subject;
        Message lastMessage = request.Messages[request.Messages.Count - 1];
        _ = lastMessage.Role.Should().Be(MessageRole.User);
        _ = ConversationPromptRunner.ExtractText(lastMessage).Should().Be(ConversationTitler.TopicPrompt);
    }

    [Fact]
    public async Task ContextSummarizerCollapsesLongHistoryToBoundedMemoryPrompt()
    {
        ScriptedProvider provider = new ScriptedProvider(CompletedText(new string('s', 9000)));
        ChatProviderRegistry registry = new ChatProviderRegistry();
        registry.Register(provider);
        ContextSummarizer summarizer = new ContextSummarizer(registry);
        Conversation conversation = CreateConversation("Existing", CreateMessages(500));
        ModelDescriptor descriptor = new ModelDescriptor(
            "gpt-4o-mini",
            "gpt-4o-mini",
            "OpenAI",
            true,
            0,
            ImmutableArray<ProviderCapability>.Empty,
            ContextWindow: 100);

        Conversation summarized = await summarizer.SummarizeAsync(conversation, descriptor, CancellationToken.None).ConfigureAwait(true);

        _ = Encoding.UTF8.GetByteCount(summarized.MemoryPrompt).Should().BeLessThanOrEqualTo(ContextSummarizer.MaxSummaryUtf8Bytes);
        _ = summarized.LastSummarizeIndex.Should().Be(500);
        ChatProviderRequest request = provider.Requests.Should().ContainSingle().Subject;
        Message lastMessage = request.Messages[request.Messages.Count - 1];
        _ = lastMessage.Role.Should().Be(MessageRole.System);
        _ = ConversationPromptRunner.ExtractText(lastMessage).Should().Be(ContextSummarizer.SummaryPrompt);
    }

    private static Message[] CreateMessages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => Message.Text(
                "m" + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                index % 2 == 0 ? MessageRole.User : MessageRole.Assistant,
                "message " + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + new string('x', 120),
                "0"))
            .ToArray();
    }

    private static Func<ChatProviderRequest, CancellationToken, IAsyncEnumerable<ChatEvent>> CompletedText(string text)
    {
        return (request, cancellationToken) => StreamCompletedText(request, text, cancellationToken);
    }

    private static async IAsyncEnumerable<ChatEvent> StreamCompletedText(
        ChatProviderRequest request,
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(true);
        yield return new MessageCompleted(
            request.ConversationId,
            request.MessageId,
            Message.Text(request.MessageId, MessageRole.Assistant, text, "0"));
        yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private static Conversation CreateConversation(string topic, params Message[] messages)
    {
        return new Conversation(
            "c1",
            topic,
            string.Empty,
            messages.ToImmutableArray(),
            new ChatStat(0, 0, 0),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            0,
            null,
            new Mask(
                "mask",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                "1f600",
                "Mask",
                false,
                ImmutableArray<Message>.Empty,
                true,
                ModelConfig.NextChatDefault with { ProviderName = "OpenAI" },
                "en",
                false,
                ImmutableArray<string>.Empty));
    }

    private sealed class ScriptedProvider : IChatProvider
    {
        private readonly Func<ChatProviderRequest, CancellationToken, IAsyncEnumerable<ChatEvent>> streamFactory;

        public ScriptedProvider(Func<ChatProviderRequest, CancellationToken, IAsyncEnumerable<ChatEvent>> streamFactory)
        {
            this.streamFactory = streamFactory;
        }

        public List<ChatProviderRequest> Requests { get; } = new List<ChatProviderRequest>();

        public string Id => "OpenAI";

        public bool SupportsVision => false;

        public IAsyncEnumerable<ChatEvent> StreamAsync(ChatProviderRequest request, CancellationToken cancellationToken = default)
        {
            this.Requests.Add(request);
            return this.streamFactory(request, cancellationToken);
        }
    }
}
