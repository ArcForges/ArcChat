// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.Agent;

/// <summary>
/// Provider-backed implementation of long-context memory summarization.
/// </summary>
public sealed class ContextSummarizer : IContextSummarizer
{
    /// <summary>
    /// Maximum stored memory prompt size, in UTF-8 bytes.
    /// </summary>
    public const int MaxSummaryUtf8Bytes = 8 * 1024;

    /// <summary>
    /// Prompt used by NextChat for context summarization.
    /// </summary>
    public const string SummaryPrompt = "Summarize the discussion briefly in 200 words or less to use as a prompt for future context.";

    private const int DefaultContextWindow = 4000;
    private readonly IChatProviderRegistry providerRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextSummarizer"/> class.
    /// </summary>
    /// <param name="providerRegistry">Provider registry used to call the active provider.</param>
    public ContextSummarizer(IChatProviderRegistry providerRegistry)
    {
        this.providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    /// <inheritdoc />
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    public async Task<Conversation> SummarizeAsync(
        Conversation conversation,
        ModelDescriptor model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(model);

        if (!conversation.Mask.ModelConfig.SendMemory)
        {
            return conversation;
        }

        int startIndex = Math.Max(conversation.LastSummarizeIndex, conversation.ClearContextIndex ?? 0);
        ImmutableArray<Message> messagesToSummarize = ConversationPromptRunner.NonErrorMessages(conversation.Messages.Skip(startIndex));
        double tokensInWindow = CountTokens(messagesToSummarize);
        int threshold = ResolveThreshold(model);
        if (tokensInWindow <= threshold)
        {
            return conversation;
        }

        Message[] promptMessages = messagesToSummarize
            .Append(ConversationPromptRunner.PromptMessage(MessageRole.System, SummaryPrompt))
            .ToArray();
        string summary = await ConversationPromptRunner
            .RunAsync(this.providerRegistry, conversation.Id, promptMessages, conversation.Mask.ModelConfig, cancellationToken)
            .ConfigureAwait(false);
        string boundedSummary = LimitUtf8(summary.Trim(), MaxSummaryUtf8Bytes);

        return conversation with
        {
            MemoryPrompt = boundedSummary,
            LastSummarizeIndex = conversation.Messages.Length,
            LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }

    internal static double CountTokens(IEnumerable<Message> messages)
    {
        return messages.Sum(message => EstimateTokenLength(ConversationPromptRunner.ExtractText(message)));
    }

    private static int ResolveThreshold(ModelDescriptor model)
    {
        return model.ContextWindow is > 0 ? model.ContextWindow.Value : DefaultContextWindow;
    }

    private static double EstimateTokenLength(string input)
    {
        double tokenLength = 0;
        foreach (char character in input)
        {
            tokenLength += character switch
            {
                >= 'A' and <= 'z' => 0.25,
                < (char)128 => 0.5,
                _ => 1.5,
            };
        }

        return tokenLength;
    }

    private static string LimitUtf8(string value, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
        {
            return value;
        }

        StringBuilder builder = new StringBuilder(value.Length);
        int bytes = 0;
        foreach (Rune rune in value.EnumerateRunes())
        {
            string text = rune.ToString();
            int nextBytes = Encoding.UTF8.GetByteCount(text);
            if (bytes + nextBytes > maxBytes)
            {
                break;
            }

            _ = builder.Append(text);
            bytes += nextBytes;
        }

        return builder.ToString();
    }
}
