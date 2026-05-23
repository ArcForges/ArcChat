// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core.Internal;

internal sealed class EchoProvider : IChatProvider
{
    private const int ChunkSize = 8;
    private static readonly TimeSpan ChunkDelay = TimeSpan.FromMilliseconds(5);

    public EchoProvider(ProviderId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Provider id must not be empty.", nameof(id));
        }

        this.Id = id;
    }

    public ProviderId Id { get; }

    public ChatProviderCapabilities Capabilities => ChatProviderCapabilities.Streaming;

    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string response = $"Echo: {GetLastUserText(request.History)}";
        foreach (string delta in Chunk(response))
        {
            await Task.Delay(ChunkDelay, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield return new MessageDelta(request.Extra.ConversationId, request.Extra.MessageId, delta);
        }

        Message message = new Message(
            request.Extra.MessageId,
            MessageRole.Assistant,
            ImmutableArray.Create<ContentBlock>(new TextBlock(response)),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Model: request.Config.Model,
            Tools: ImmutableArray<ChatMessageTool>.Empty);

        yield return new MessageCompleted(request.Extra.ConversationId, request.Extra.MessageId, message);
        yield return new ChatFinished(request.Extra.ConversationId, request.Extra.MessageId, "stop");
    }

    public Task<ImmutableArray<ModelDescriptor>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        ImmutableArray<ModelDescriptor> models = ImmutableArray.Create(
            new ModelDescriptor(
                "echo",
                "Echo",
                this.Id.Value,
                true,
                0,
                ImmutableArray.Create<ProviderCapability>(new StreamingCapability())));
        return Task.FromResult(models);
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
