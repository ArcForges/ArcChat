// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.ModelProviders.Core.Internal;
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
        EchoProvider provider = new EchoProvider("Echo");
        ChatProviderRequest request = new ChatProviderRequest(
            "c1",
            "m1",
            new[] { Message.Text("u1", MessageRole.User, "hello", "0") },
            ModelConfig.NextChatDefault);

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
