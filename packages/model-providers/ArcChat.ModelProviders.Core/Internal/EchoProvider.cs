// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using ArcChat.Protocol.Chat;

namespace ArcChat.ModelProviders.Core.Internal;

internal sealed class EchoProvider : IChatProvider
{
    private const int ChunkSize = 8;
    private static readonly TimeSpan ChunkDelay = TimeSpan.FromMilliseconds(5);

    public EchoProvider(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        this.Id = id;
    }

    public string Id { get; }

    public bool SupportsVision => false;

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatProviderRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string response = $"Echo: {GetLastUserText(request.Messages)}";
        foreach (string delta in Chunk(response))
        {
            await Task.Delay(ChunkDelay, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield return new MessageDelta(request.ConversationId, request.MessageId, delta);
        }

        Message message = new Message(
            request.MessageId,
            MessageRole.Assistant,
            ImmutableArray.Create<ContentBlock>(new TextBlock(response)),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Model: request.Model.Model,
            Tools: ImmutableArray<ChatMessageTool>.Empty);

        yield return new MessageCompleted(request.ConversationId, request.MessageId, message);
        yield return new ChatFinished(request.ConversationId, request.MessageId, "stop");
    }

    private static string GetLastUserText(IReadOnlyList<Message> messages)
    {
        Message? userMessage = messages.LastOrDefault(message => message.Role == MessageRole.User);
        if (userMessage is null)
        {
            return string.Empty;
        }

        return string.Concat(userMessage.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    private static IEnumerable<string> Chunk(string text)
    {
        for (int index = 0; index < text.Length; index += ChunkSize)
        {
            yield return text.Substring(index, Math.Min(ChunkSize, text.Length - index));
        }
    }
}
