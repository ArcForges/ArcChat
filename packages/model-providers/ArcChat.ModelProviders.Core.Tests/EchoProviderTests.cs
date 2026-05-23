// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.ModelProviders.Core.Internal;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using Xunit;

namespace ArcChat.ModelProviders.Core.Tests;

public sealed class EchoProviderTests
{
    [Fact]
    public async Task StreamAsyncEchoesLastUserMessageAsOrderedChatEvents()
    {
        EchoProvider provider = new EchoProvider(new ProviderId("Echo"));
        ChatRequest request = new ChatRequest(
            new[] { Message.Text("u1", MessageRole.User, "hello", "0") }.ToImmutableArray(),
            ModelConfig.NextChatDefault,
            ImmutableArray<ArcTool>.Empty,
            ProviderExtra.ForStream("c1", "m1"));

        List<ChatEvent> events = new List<ChatEvent>();
        await foreach (ChatEvent chatEvent in provider.StreamAsync(request).ConfigureAwait(false))
        {
            events.Add(chatEvent);
        }

        _ = events.OfType<MessageDelta>().Select(static delta => delta.Delta).Should().ContainInOrder("Echo: he", "llo");
        _ = events.OfType<MessageCompleted>().Single().Message.Content.OfType<TextBlock>().Single().Text.Should().Be("Echo: hello");
        _ = events.Last().Should().BeOfType<ChatFinished>();
    }
}
