// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigMathBlock = Markdig.Extensions.Mathematics.MathBlock;

namespace ArcChat.UI.Markdown.Markdown.Text;

/// <summary>
/// Markdig-backed plain text renderer for export flows.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsShouldAppearBeforeInstanceElements", Justification = "Append helper is kept after rendering methods for readability.")]
public sealed class MarkdownTextRenderer
{
    private readonly MarkdownPipeline pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownTextRenderer"/> class.
    /// </summary>
    public MarkdownTextRenderer()
        : this(MarkdownPipelineFactory.Create())
    {
    }

    internal MarkdownTextRenderer(MarkdownPipeline pipeline)
    {
        this.pipeline = pipeline;
    }

    /// <summary>
    /// Renders Markdown to readable plain text while preserving code and table content.
    /// </summary>
    public string Render(string markdown)
    {
        string normalized = NextChatMarkdownPreprocessor.Normalize(markdown ?? string.Empty);
        MarkdownDocument document = Markdig.Markdown.Parse(normalized, this.pipeline);
        StringBuilder builder = new StringBuilder(normalized.Length);
        foreach (Block block in document)
        {
            this.RenderBlock(block, builder);
        }

        return builder.ToString().Trim();
    }

    private void RenderBlock(Block block, StringBuilder builder)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AppendSeparated(builder, this.RenderInline(heading.Inline));
                break;
            case ParagraphBlock paragraph:
                AppendSeparated(builder, this.RenderInline(paragraph.Inline));
                break;
            case QuoteBlock quote:
                foreach (Block child in quote)
                {
                    this.RenderBlock(child, builder);
                }

                break;
            case ListBlock list:
                this.RenderList(list, builder);
                break;
            case MarkdigMathBlock math:
                AppendSeparated(builder, math.Lines.ToString().Trim());
                break;
            case HtmlBlock html:
                AppendSeparated(builder, html.Lines.ToString().TrimEnd());
                break;
            case FencedCodeBlock fenced:
                AppendSeparated(builder, fenced.Lines.ToString().TrimEnd());
                break;
            case Markdig.Syntax.CodeBlock code:
                AppendSeparated(builder, code.Lines.ToString().TrimEnd());
                break;
            case Table table:
                this.RenderTable(table, builder);
                break;
            case ThematicBreakBlock:
                AppendSeparated(builder, "---");
                break;
            default:
                AppendSeparated(builder, block.ToString() ?? string.Empty);
                break;
        }
    }

    private void RenderList(ListBlock list, StringBuilder builder)
    {
        int index = int.TryParse(list.OrderedStart, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int orderedStart)
            ? orderedStart
            : 1;
        StringBuilder itemBuilder = new StringBuilder();
        foreach (Block item in list)
        {
            _ = itemBuilder.Clear();
            foreach (LeafBlock leaf in item.Descendants<LeafBlock>())
            {
                if (itemBuilder.Length > 0)
                {
                    _ = itemBuilder.Append(' ');
                }

                _ = itemBuilder.Append(this.RenderInline(leaf.Inline));
            }

            string prefix = list.IsOrdered ? index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ". " : "- ";
            AppendSeparated(builder, prefix + itemBuilder.ToString().Trim());
            index++;
        }
    }

    private void RenderTable(Table table, StringBuilder builder)
    {
        foreach (TableRow row in table.OfType<TableRow>())
        {
            string line = string.Join(
                " | ",
                row.OfType<TableCell>().Select(cell => string.Join(" ", cell.Descendants<LeafBlock>().Select(leaf => this.RenderInline(leaf.Inline)))));
            AppendSeparated(builder, line);
        }
    }

    private string RenderInline(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        return string.Concat(inline.Select(this.RenderInlineObject));
    }

    private string RenderInlineObject(Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => literal.Content.ToString(),
            LineBreakInline => Environment.NewLine,
            CodeInline code => code.Content,
            EmphasisInline emphasis => this.RenderInline(emphasis),
            LinkInline link when link.IsImage => link.Url ?? string.Empty,
            LinkInline link => this.RenderInline(link),
            Markdig.Extensions.Mathematics.MathInline math => math.Content.ToString(),
            HtmlInline html => html.Tag,
            _ => inline.ToString() ?? string.Empty,
        };
    }

    private static void AppendSeparated(StringBuilder builder, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (builder.Length > 0)
        {
            _ = builder.AppendLine();
            _ = builder.AppendLine();
        }

        _ = builder.Append(text.Trim());
    }
}
