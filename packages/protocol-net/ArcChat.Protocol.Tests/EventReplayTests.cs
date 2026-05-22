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
        string path = GetFixturePath(fixtureName);
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

    private static string GetFixturePath(string fixtureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureName);
        if (Path.IsPathRooted(fixtureName) || Path.IsPathFullyQualified(fixtureName))
        {
            throw new ArgumentException("Fixture name must be relative.", nameof(fixtureName));
        }

        string fixturesDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", "Fixtures"));
        string fixturesRoot = Path.EndsInDirectorySeparator(fixturesDirectory)
            ? fixturesDirectory
            : fixturesDirectory + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(fixtureName, fixturesRoot);

        if (!path.StartsWith(fixturesRoot, StringComparison.Ordinal))
        {
            throw new ArgumentException("Fixture path must stay under the fixture directory.", nameof(fixtureName));
        }

        return path;
    }
}
