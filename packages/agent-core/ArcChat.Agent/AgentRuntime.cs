// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text.Json;
using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Chat;

namespace ArcChat.Agent;

/// <summary>
/// Default ArcChat agent runtime that streams provider events and executes the NC04 no-op tool loop.
/// </summary>
public sealed class AgentRuntime : IAgentRuntime
{
    private readonly IChatProviderRegistry providerRegistry;
    private readonly IAgentToolRegistry toolRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRuntime"/> class.
    /// </summary>
    /// <param name="providerRegistry">Provider registry used to resolve <c>ModelConfig.ProviderName</c>.</param>
    /// <param name="toolRegistry">Tool registry used when providers emit tool call events.</param>
    public AgentRuntime(IChatProviderRegistry providerRegistry, IAgentToolRegistry? toolRegistry = null)
    {
        this.providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        this.toolRegistry = toolRegistry ?? NoOpAgentToolRegistry.Instance;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatEvent> StreamAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MaxTransientRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "MaxTransientRetries cannot be negative.");
        }

        int attempt = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StreamAttemptResult result = await this.StreamAttemptAsync(request, cancellationToken).ConfigureAwait(false);
            foreach (ChatEvent chatEvent in result.Events)
            {
                yield return chatEvent;
            }

            if (result.Exception is null)
            {
                yield break;
            }

            if (IsTransientNetworkFailure(result.Exception) && attempt < request.MaxTransientRetries)
            {
                attempt++;
                if (request.TransientRetryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(request.TransientRetryDelay, cancellationToken).ConfigureAwait(false);
                }

                continue;
            }

            yield return new ChatError(
                request.ConversationId,
                request.MessageId,
                IsTransientNetworkFailure(result.Exception) ? "network.transient" : "agent.provider",
                result.Exception.Message);
            yield return new ChatFinished(request.ConversationId, request.MessageId, "error");
            yield break;
        }
    }

    private static bool IsTransientNetworkFailure(Exception exception)
    {
        return exception is IOException or TimeoutException or HttpRequestException;
    }

    private static JsonElement JsonObject(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static AgentToolRequest ToAgentToolRequest(ChatMessageTool tool)
    {
        JsonElement arguments = JsonObject("{}");
        if (!string.IsNullOrWhiteSpace(tool.Arguments))
        {
            try
            {
                arguments = JsonObject(tool.Arguments);
            }
            catch (JsonException)
            {
                arguments = JsonObject("{}");
            }
        }

        return new AgentToolRequest(tool.Id, tool.Name, arguments);
    }

    private async Task<StreamAttemptResult> StreamAttemptAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        List<ChatEvent> events = new List<ChatEvent>();
        ChatProviderRequest providerRequest = new ChatProviderRequest(
            request.ConversationId,
            request.MessageId,
            request.Messages,
            request.Model);

        IChatProvider provider = this.providerRegistry.Resolve(request.Model.ProviderName);
        IAsyncEnumerator<ChatEvent> enumerator = provider
            .StreamAsync(providerRequest, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    return new StreamAttemptResult(events, exception);
                }

                if (!hasNext)
                {
                    return new StreamAttemptResult(events);
                }

                cancellationToken.ThrowIfCancellationRequested();
                await foreach (ChatEvent processedEvent in this.ProcessEventAsync(enumerator.Current, cancellationToken).ConfigureAwait(false))
                {
                    events.Add(processedEvent);
                }
            }
        }
    }

    private async IAsyncEnumerable<ChatEvent> ProcessEventAsync(
        ChatEvent chatEvent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return chatEvent;

        if (chatEvent is not ToolCallStarted toolCallStarted)
        {
            yield break;
        }

        AgentToolResult result = await this.toolRegistry
            .ExecuteAsync(ToAgentToolRequest(toolCallStarted.Tool), cancellationToken)
            .ConfigureAwait(false);

        ChatMessageTool completedTool = new ChatMessageTool(
            toolCallStarted.Tool.Id,
            toolCallStarted.Tool.Name,
            toolCallStarted.Tool.Arguments,
            toolCallStarted.Tool.Index,
            toolCallStarted.Tool.Type,
            result.Result.GetRawText(),
            result.IsError,
            result.ErrorMessage);

        yield return new ToolCallCompleted(
            toolCallStarted.ConversationId,
            toolCallStarted.MessageId,
            completedTool,
            result.Result);
    }

    private sealed class StreamAttemptResult
    {
        public StreamAttemptResult(IReadOnlyList<ChatEvent> events, Exception? exception = null)
        {
            this.Events = events;
            this.Exception = exception;
        }

        public IReadOnlyList<ChatEvent> Events { get; }

        public Exception? Exception { get; }
    }
}
