// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Concurrent;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using SharpToken;

namespace ArcChat.ModelProviders.Tokenizer;

/// <summary>
/// NextChat-compatible token counter for provider model descriptors.
/// </summary>
public sealed class TokenCounter : ITokenCounter
{
    private const string Cl100KBase = "cl100k_base";
    private const string O200KBase = "o200k_base";

    private static readonly ConcurrentDictionary<string, GptEncoding> Encodings =
        new ConcurrentDictionary<string, GptEncoding>(StringComparer.Ordinal);

    /// <inheritdoc />
    public int Count(IEnumerable<Message> messages, ModelDescriptor model)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(model);

        int total = 0;
        foreach (Message message in messages)
        {
            string text = GetMessageTextContent(message);
            total += CountText(text, model);
        }

        return total;
    }

    private static string GetMessageTextContent(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.Content.OfType<TextBlock>().FirstOrDefault()?.Text ?? string.Empty;
    }

    private static int CountNextChatEstimatedTokens(string text)
    {
        double tokenLength = 0;
        foreach (char character in text)
        {
            int charCode = character;
            if (charCode < 128)
            {
                tokenLength += charCode is >= 65 and <= 122 ? 0.25 : 0.5;
            }
            else
            {
                tokenLength += 1.5;
            }
        }

        return (int)Math.Ceiling(tokenLength);
    }

    private static GptEncoding GetOpenAiEncoding(ModelDescriptor model)
    {
        string encodingName = SelectOpenAiEncodingName(model.Id);
        return Encodings.GetOrAdd(encodingName, static name => GptEncoding.GetEncoding(name));
    }

    private static bool IsOpenAiModel(ModelDescriptor model)
    {
        return IsProvider(model, "openai") || IsProvider(model, "azure");
    }

    private static bool IsProvider(ModelDescriptor model, string providerId)
    {
        return string.Equals(model.ProviderId, providerId, StringComparison.OrdinalIgnoreCase);
    }

    private static string SelectOpenAiEncodingName(string modelId)
    {
        if (modelId.StartsWith("gpt-4o", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("gpt-4.1", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o1", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o3", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o4", StringComparison.OrdinalIgnoreCase))
        {
            return O200KBase;
        }

        return Cl100KBase;
    }

    private static int CountText(string text, ModelDescriptor model)
    {
        if (IsOpenAiModel(model))
        {
            return GetOpenAiEncoding(model).CountTokens(text);
        }

        return CountNextChatEstimatedTokens(text);
    }
}
