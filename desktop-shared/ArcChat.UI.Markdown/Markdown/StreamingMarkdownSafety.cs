// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Markdown.Markdown;

internal static class StreamingMarkdownSafety
{
    private static readonly string[] LineSeparators = { "\r\n", "\n" };

    public static PreparedMarkdown Prepare(string markdown, bool isStreaming)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (!isStreaming)
        {
            return new PreparedMarkdown(markdown, Array.Empty<MarkdownPlaceholder>());
        }

        if (FindUnclosedFence(markdown) is { } fence)
        {
            MarkdownPlaceholderKind kind = IsMermaidLanguage(fence.Language)
                ? MarkdownPlaceholderKind.Mermaid
                : MarkdownPlaceholderKind.Code;
            return new PreparedMarkdown(
                markdown[..fence.Start],
                new[] { new MarkdownPlaceholder(kind, fence.Language, markdown[fence.Start..], true) });
        }

        int? math = FindUnclosedMath(markdown);
        if (math.HasValue)
        {
            return new PreparedMarkdown(
                markdown[..math.Value],
                new[] { new MarkdownPlaceholder(MarkdownPlaceholderKind.Math, string.Empty, markdown[math.Value..], true) });
        }

        int? table = FindPartialTable(markdown);
        if (table.HasValue)
        {
            return new PreparedMarkdown(
                markdown[..table.Value],
                new[] { new MarkdownPlaceholder(MarkdownPlaceholderKind.Table, string.Empty, markdown[table.Value..], true) });
        }

        return new PreparedMarkdown(markdown, Array.Empty<MarkdownPlaceholder>());
    }

    private static FenceState? FindUnclosedFence(string markdown)
    {
        bool inFence = false;
        int fenceStart = 0;
        string fenceMarker = string.Empty;
        string language = string.Empty;
        int lineStart = 0;
        while (lineStart <= markdown.Length)
        {
            int lineEnd = markdown.IndexOf('\n', lineStart);
            bool hasLineBreak = lineEnd >= 0;
            string line = hasLineBreak ? markdown[lineStart..lineEnd] : markdown[lineStart..];
            string trimmed = line.TrimStart();
            string? marker = GetFenceMarker(trimmed);
            if (marker is not null)
            {
                if (!inFence)
                {
                    inFence = true;
                    fenceStart = lineStart;
                    fenceMarker = marker;
                    language = trimmed[marker.Length..].Trim();
                }
                else if (trimmed.StartsWith(fenceMarker, StringComparison.Ordinal))
                {
                    inFence = false;
                    fenceMarker = string.Empty;
                    language = string.Empty;
                }
            }

            if (!hasLineBreak)
            {
                break;
            }

            lineStart = lineEnd + 1;
        }

        return inFence ? new FenceState(fenceStart, language) : null;
    }

    private static string? GetFenceMarker(string trimmedLine)
    {
        if (trimmedLine.Length < 3)
        {
            return null;
        }

        if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
        {
            return "```";
        }

        return trimmedLine.StartsWith("~~~", StringComparison.Ordinal) ? "~~~" : null;
    }

    private static int? FindUnclosedMath(string markdown)
    {
        int lastStart = -1;
        int index = 0;
        while (index < markdown.Length - 1)
        {
            if (markdown[index] == '$' && markdown[index + 1] == '$')
            {
                lastStart = lastStart < 0 ? index : -1;
                index += 2;
                continue;
            }

            index++;
        }

        if (lastStart >= 0)
        {
            return lastStart;
        }

        int bracketStart = markdown.LastIndexOf("\\[", StringComparison.Ordinal);
        int bracketEnd = markdown.LastIndexOf("\\]", StringComparison.Ordinal);
        return bracketStart > bracketEnd ? bracketStart : null;
    }

    private static int? FindPartialTable(string markdown)
    {
        if (markdown.EndsWith("\n\n", StringComparison.Ordinal) || markdown.EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            return null;
        }

        int start = Math.Max(
            markdown.LastIndexOf("\n\n", StringComparison.Ordinal),
            markdown.LastIndexOf("\r\n\r\n", StringComparison.Ordinal));
        start = start < 0 ? 0 : start + 2;
        string tail = markdown[start..].Trim('\r', '\n');
        if (tail.Length == 0)
        {
            return null;
        }

        string[] lines = tail.Split(LineSeparators, StringSplitOptions.None);
        return lines.Length >= 2 && lines[0].Contains('|', StringComparison.Ordinal) && IsTableSeparator(lines[1])
            ? start
            : null;
    }

    private static bool IsTableSeparator(string line)
    {
        string trimmed = line.Trim();
        if (!trimmed.Contains('-', StringComparison.Ordinal) || !trimmed.Contains('|', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char character in trimmed)
        {
            if (character is not '|' and not '-' and not ':' and not ' ')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMermaidLanguage(string language)
    {
        return string.Equals(language.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), "mermaid", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FenceState
    {
        public FenceState(int start, string language)
        {
            this.Start = start;
            this.Language = language;
        }

        public int Start { get; }

        public string Language { get; }
    }
}
