// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using FluentAssertions;

namespace ArcChat.ModelProviders.Core.ContractTestKit;

/// <summary>
/// Shared offline contract assertions for chat provider fixtures.
/// </summary>
public static class ChatProviderContractAssertions
{
    /// <summary>
    /// Collects all events from a provider stream.
    /// </summary>
    public static async Task<IReadOnlyList<ChatEvent>> CollectAsync(
        IChatProvider provider,
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(request);

        List<ChatEvent> events = new List<ChatEvent>();
        await foreach (ChatEvent chatEvent in provider.StreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            events.Add(chatEvent);
        }

        return events;
    }

    /// <summary>
    /// Asserts that stream text deltas contain the expected final text.
    /// </summary>
    public static void ShouldStreamText(IReadOnlyList<ChatEvent> events, string expectedText)
    {
        ArgumentNullException.ThrowIfNull(events);

        string text = string.Concat(events.OfType<MessageDelta>().Select(delta => delta.Delta));
        _ = text.Should().Be(expectedText);
        _ = events.OfType<MessageCompleted>().Should().ContainSingle();
    }

    /// <summary>
    /// Asserts that a provider stream emitted a reasoning delta.
    /// </summary>
    public static void ShouldStreamReasoning(IReadOnlyList<ChatEvent> events, string expectedReasoning)
    {
        ArgumentNullException.ThrowIfNull(events);

        string text = string.Concat(events.OfType<ReasoningDelta>().Select(delta => delta.Delta));
        _ = text.Should().Be(expectedReasoning);
    }

    /// <summary>
    /// Asserts that a provider stream emitted a complete tool call.
    /// </summary>
    public static void ShouldCompleteToolCall(IReadOnlyList<ChatEvent> events, string name, string arguments)
    {
        ArgumentNullException.ThrowIfNull(events);

        ToolCallCompleted completed = events.OfType<ToolCallCompleted>().Should().ContainSingle().Subject;
        _ = completed.Tool.Name.Should().Be(name);
        _ = completed.Tool.Arguments.Should().Be(arguments);
    }

    /// <summary>
    /// Asserts that a provider stream ended with the given finish reason.
    /// </summary>
    public static void ShouldFinish(IReadOnlyList<ChatEvent> events, string? finishReason)
    {
        ArgumentNullException.ThrowIfNull(events);

        ChatFinished finished = events.OfType<ChatFinished>().Should().ContainSingle().Subject;
        _ = finished.FinishReason.Should().Be(finishReason);
    }

    /// <summary>
    /// Asserts that the provider maps a transport failure into a chat error event.
    /// </summary>
    public static void ShouldContainError(IReadOnlyList<ChatEvent> events, string code)
    {
        ArgumentNullException.ThrowIfNull(events);

        ChatError error = events.OfType<ChatError>().Should().ContainSingle().Subject;
        _ = error.Code.Should().Be(code);
    }
}
