// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.ModelProviders.Core;
using ArcChat.Protocol.Chat;

namespace ArcChat.Agent;

/// <summary>
/// Provider-backed implementation of conversation title generation.
/// </summary>
public sealed class ConversationTitler : IConversationTitler
{
    /// <summary>
    /// NextChat's default untitled conversation topic.
    /// </summary>
    public const string DefaultTopic = "New Conversation";

    /// <summary>
    /// Prompt used by NextChat for title generation.
    /// </summary>
    public const string TopicPrompt = "Please generate a four to five word title summarizing our conversation without any lead-in, punctuation, quotation marks, periods, symbols, bold text, or additional text. Remove enclosing quotation marks.";

    private static readonly char[] TopicBoundaryCharacters = new[] { '"', '“', '”', '*' };
    private static readonly char[] TopicTrailingPunctuationCharacters = new[] { '，', '。', '！', '？', '”', '“', '"', '、', ',', '.', '!', '?', '*' };
    private readonly IChatProviderRegistry providerRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationTitler"/> class.
    /// </summary>
    /// <param name="providerRegistry">Provider registry used to call the active provider.</param>
    public ConversationTitler(IChatProviderRegistry providerRegistry)
    {
        this.providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    /// <summary>
    /// Checks whether a conversation still has the default generated-title target.
    /// </summary>
    /// <param name="topic">Current conversation topic.</param>
    /// <returns><see langword="true"/> when title generation may replace the topic.</returns>
    public static bool IsDefaultTopic(string topic)
    {
        return string.IsNullOrWhiteSpace(topic)
            || string.Equals(topic, DefaultTopic, StringComparison.Ordinal)
            || string.Equals(topic, "Chat", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public async Task<string> GenerateTitleAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (!HasCompletedRoundTrip(conversation))
        {
            return string.Empty;
        }

        Message[] messages = ConversationPromptRunner
            .NonErrorMessages(conversation.Messages)
            .Append(ConversationPromptRunner.PromptMessage(MessageRole.User, TopicPrompt))
            .ToArray();
        string title = await ConversationPromptRunner
            .RunAsync(this.providerRegistry, conversation.Id, messages, conversation.Mask.ModelConfig, cancellationToken)
            .ConfigureAwait(false);
        return TrimTopic(title);
    }

    private static bool HasCompletedRoundTrip(Conversation conversation)
    {
        return conversation.Messages.Any(static message => message.Role == MessageRole.User && !message.IsError)
            && conversation.Messages.Any(static message => message.Role == MessageRole.Assistant && !message.IsError && !message.Streaming);
    }

    private static string TrimTopic(string topic)
    {
        return topic
            .Trim()
            .Trim(TopicBoundaryCharacters)
            .TrimEnd(TopicTrailingPunctuationCharacters)
            .Trim();
    }
}
