// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Markdown.Markdown;

internal sealed class PreparedMarkdown
{
    public PreparedMarkdown(string markdown, IReadOnlyList<MarkdownPlaceholder> placeholders)
    {
        this.Markdown = markdown;
        this.Placeholders = placeholders;
    }

    public string Markdown { get; }

    public IReadOnlyList<MarkdownPlaceholder> Placeholders { get; }
}
