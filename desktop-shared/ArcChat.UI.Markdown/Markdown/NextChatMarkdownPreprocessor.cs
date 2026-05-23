// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text;

namespace ArcChat.UI.Markdown.Markdown;

internal static class NextChatMarkdownPreprocessor
{
    public static string Normalize(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return EscapeBracketMath(markdown);
    }

    private static string EscapeBracketMath(string markdown)
    {
        StringBuilder builder = new StringBuilder(markdown.Length);
        bool inFence = false;
        int lineStart = 0;
        while (lineStart <= markdown.Length)
        {
            int lineEnd = markdown.IndexOf('\n', lineStart);
            bool hasLineBreak = lineEnd >= 0;
            string line = hasLineBreak ? markdown[lineStart..lineEnd] : markdown[lineStart..];
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }

            builder.Append(inFence ? line : ReplaceMathDelimiters(line));
            if (!hasLineBreak)
            {
                break;
            }

            builder.Append('\n');
            lineStart = lineEnd + 1;
        }

        return builder.ToString();
    }

    private static string ReplaceMathDelimiters(string line)
    {
        return line
            .Replace("\\[", "$$", StringComparison.Ordinal)
            .Replace("\\]", "$$", StringComparison.Ordinal)
            .Replace("\\(", "$", StringComparison.Ordinal)
            .Replace("\\)", "$", StringComparison.Ordinal);
    }
}
