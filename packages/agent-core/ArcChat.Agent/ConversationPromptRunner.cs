// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.Agent;

internal static class ConversationPromptRunner
{
    internal static Message PromptMessage(MessageRole role, string text)
    {
        return Message.Text(
            Guid.NewGuid().ToString("N"),
            role,
            text,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
    }

    internal static async Task<string> RunAsync(
        IChatProviderRegistry providerRegistry,
        string conversationId,
        IReadOnlyList<Message> messages,
        ModelConfig model,
        CancellationToken cancellationToken)
    {
        IChatProvider provider = providerRegistry.Resolve(new ProviderId(model.ProviderName), model);
        string messageId = Guid.NewGuid().ToString("N");
        ChatRequest request = new ChatRequest(
            messages.ToImmutableArray(),
            model with { Stream = false },
            ImmutableArray<ArcTool>.Empty,
            ProviderExtra.ForStream(conversationId, messageId));
        StringBuilder builder = new StringBuilder();

        await foreach (ChatEvent chatEvent in provider.StreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            switch (chatEvent)
            {
                case MessageDelta delta:
                    _ = builder.Append(delta.Delta);
                    break;
                case MessageCompleted completed:
                    builder.Clear();
                    _ = builder.Append(ExtractText(completed.Message));
                    break;
            }
        }

        return builder.ToString();
    }

    internal static string ExtractText(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    internal static ImmutableArray<Message> NonErrorMessages(IEnumerable<Message> messages)
    {
        return messages.Where(static message => !message.IsError).ToImmutableArray();
    }
}
