// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using ArcChat.Protocol.Chat;

namespace ArcChat.Agent.Internal;

internal sealed class MicrosoftAgentFrameworkRuntime : IAgentRuntime
{
    private readonly Microsoft.Agents.AI.AIAgent agent;

    public MicrosoftAgentFrameworkRuntime(Microsoft.Agents.AI.AIAgent agent)
    {
        this.agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Microsoft.Extensions.AI.ChatMessage[] messages = request.Messages.Select(ToChatMessage).ToArray();
        string completedText = string.Empty;
        string? finishReason = null;

        await foreach (Microsoft.Agents.AI.AgentResponseUpdate update in this.agent
            .RunStreamingAsync(messages, cancellationToken: cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(update.Text))
            {
                completedText += update.Text;
                yield return new MessageDelta(request.ConversationId, request.MessageId, update.Text);
            }

            finishReason = update.FinishReason?.Value ?? finishReason;
        }

        Message message = new Message(
            request.MessageId,
            MessageRole.Assistant,
            ImmutableArray.Create<ContentBlock>(new TextBlock(completedText)),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Model: request.Model.Model,
            Tools: ImmutableArray<ChatMessageTool>.Empty);

        yield return new MessageCompleted(request.ConversationId, request.MessageId, message);
        yield return new ChatFinished(request.ConversationId, request.MessageId, finishReason ?? "stop");
    }

    private static Microsoft.Extensions.AI.ChatMessage ToChatMessage(Message message)
    {
        Microsoft.Extensions.AI.ChatRole role = message.Role switch
        {
            MessageRole.System => Microsoft.Extensions.AI.ChatRole.System,
            MessageRole.Assistant => Microsoft.Extensions.AI.ChatRole.Assistant,
            _ => Microsoft.Extensions.AI.ChatRole.User,
        };

        return new Microsoft.Extensions.AI.ChatMessage(role, string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text)));
    }
}
