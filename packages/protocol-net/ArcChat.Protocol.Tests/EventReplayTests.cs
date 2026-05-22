// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Streaming;
using FluentAssertions;
using Xunit;

namespace ArcChat.Protocol.Tests;

public sealed class EventReplayTests
{
    [Theory]
    [InlineData("openai-stream.ndjson")]
    [InlineData("anthropic-stream.ndjson")]
    [InlineData("google-stream.ndjson")]
    public async Task ReplayPreservesEventOrderAndConversationId(string fixtureName)
    {
        if (Path.IsPathRooted(fixtureName))
        {
            throw new ArgumentException("Fixture name must be relative.", nameof(fixtureName));
        }

        string path = Path.Combine(AppContext.BaseDirectory, "Resources", "Fixtures", fixtureName);
        await using FileStream stream = File.OpenRead(path);

        List<ChatEvent> events = new List<ChatEvent>();
        await foreach (ChatEvent chatEvent in EventReplay.ReadEventsAsync(stream, CancellationToken.None))
        {
            events.Add(chatEvent);
        }

        _ = events.Should().HaveCount(4);
        _ = events.Select(e => e.ConversationId).Should().OnlyContain(id => id == "session-1");
        _ = events.Should().ContainInOrder(
            events.OfType<MessageDelta>().First(),
            events.OfType<ReasoningDelta>().First(),
            events.OfType<MessageCompleted>().First(),
            events.OfType<ChatFinished>().First());
    }
}
