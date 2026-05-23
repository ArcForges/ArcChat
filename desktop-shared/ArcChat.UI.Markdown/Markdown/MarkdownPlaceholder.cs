// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Markdown.Markdown;

internal sealed class MarkdownPlaceholder
{
    public MarkdownPlaceholder(MarkdownPlaceholderKind kind, string language, string source, bool isPartial)
    {
        this.Kind = kind;
        this.Language = language;
        this.Source = source;
        this.IsPartial = isPartial;
    }

    public MarkdownPlaceholderKind Kind { get; }

    public string Language { get; }

    public string Source { get; }

    public bool IsPartial { get; }
}
