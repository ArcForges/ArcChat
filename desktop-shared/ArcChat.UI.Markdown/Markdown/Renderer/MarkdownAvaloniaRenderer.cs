// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigMathBlock = Markdig.Extensions.Mathematics.MathBlock;

namespace ArcChat.UI.Markdown.Markdown.Renderer;

/// <summary>
/// Markdig-to-Avalonia renderer for streaming assistant Markdown.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Renderer helpers stay instance members to keep StyleCop ordering readable.")]
public sealed class MarkdownAvaloniaRenderer
{
    private readonly MarkdownPipeline pipeline;
    private readonly LatexRenderer latexRenderer;

    /// <summary>Initializes a new instance of the <see cref="MarkdownAvaloniaRenderer"/> class.</summary>
    public MarkdownAvaloniaRenderer()
        : this(MarkdownPipelineFactory.Create(), new LatexRenderer())
    {
    }

    internal MarkdownAvaloniaRenderer(MarkdownPipeline pipeline, LatexRenderer latexRenderer)
    {
        this.pipeline = pipeline;
        this.latexRenderer = latexRenderer;
    }

    /// <summary>Renders Markdown text to an Avalonia control tree.</summary>
    public Control Render(string markdown, bool isStreaming)
    {
        string normalized = NextChatMarkdownPreprocessor.Normalize(markdown);
        PreparedMarkdown prepared = StreamingMarkdownSafety.Prepare(normalized, isStreaming);
        MarkdownDocument document = Markdig.Markdown.Parse(prepared.Markdown, this.pipeline);
        StackPanel panel = new StackPanel
        {
            Spacing = 8,
        };

        foreach (Block block in document)
        {
            panel.Children.Add(this.RenderBlock(block));
        }

        foreach (MarkdownPlaceholder placeholder in prepared.Placeholders)
        {
            panel.Children.Add(this.RenderPlaceholder(placeholder));
        }

        return panel;
    }

    private Control RenderBlock(Block block)
    {
        return block switch
        {
            HeadingBlock heading => this.CreateTextBlock(this.RenderInline(heading.Inline), FontWeight.SemiBold, this.HeadingSize(heading.Level)),
            ParagraphBlock paragraph => this.CreateSelectableTextBlock(this.RenderInline(paragraph.Inline)),
            QuoteBlock quote => this.RenderQuote(quote),
            ListBlock list => this.RenderList(list),
            MarkdigMathBlock math => new MathBlock(math.Lines.ToString(), false, this.latexRenderer),
            FencedCodeBlock fencedCode => this.RenderFencedCode(fencedCode),
            Markdig.Syntax.CodeBlock code => new CodeBlock(code.Lines.ToString(), string.Empty),
            ThematicBreakBlock => new Border { Height = 1, Background = Brushes.LightGray, Margin = new Avalonia.Thickness(0, 8) },
            Table table => this.RenderTable(table),
            HtmlBlock html => new CodeBlock(html.Lines.ToString(), "html"),
            _ => this.CreateSelectableTextBlock(block.ToString() ?? string.Empty),
        };
    }

    private Control RenderPlaceholder(MarkdownPlaceholder placeholder)
    {
        return placeholder.Kind switch
        {
            MarkdownPlaceholderKind.Code => new CodeBlock(placeholder.Source, placeholder.Language, true),
            MarkdownPlaceholderKind.Math => new MathBlock(placeholder.Source, true, this.latexRenderer),
            MarkdownPlaceholderKind.Mermaid => new MermaidPlaceholder(placeholder.Source, true),
            MarkdownPlaceholderKind.Table => this.RenderTablePlaceholder(placeholder.Source),
            _ => this.CreateSelectableTextBlock(placeholder.Source),
        };
    }

    private Control RenderFencedCode(FencedCodeBlock code)
    {
        string language = code.Info ?? string.Empty;
        string source = code.Lines.ToString();
        if (string.Equals(language.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            return new MermaidPlaceholder(source, false);
        }

        return new CodeBlock(source, language);
    }

    private Border RenderQuote(ContainerBlock quote)
    {
        StackPanel panel = new StackPanel { Spacing = 6 };
        foreach (Block child in quote)
        {
            panel.Children.Add(this.RenderBlock(child));
        }

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Avalonia.Thickness(4, 0, 0, 0),
            Padding = new Avalonia.Thickness(12, 0, 0, 0),
            Child = panel,
        };
    }

    private ItemsControl RenderList(ListBlock list)
    {
        List<string> items = new List<string>();
        int index = int.TryParse(list.OrderedStart, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int orderedStart)
            ? orderedStart
            : 1;
        foreach (Block item in list)
        {
            string prefix = list.IsOrdered ? index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ". " : "- ";
            items.Add(prefix + string.Join(" ", item.Descendants<LeafBlock>().Select(leaf => this.RenderInline(leaf.Inline))));
            index++;
        }

        return new ItemsControl
        {
            ItemsSource = items,
        };
    }

    private Border RenderTable(Table table)
    {
        ItemsControl items = new ItemsControl
        {
            ItemsSource = table
                .OfType<TableRow>()
                .Select(row => string.Join(" | ", row.OfType<TableCell>().Select(cell => string.Join(" ", cell.Descendants<LeafBlock>().Select(leaf => this.RenderInline(leaf.Inline))))))
                .ToArray(),
        };

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(8),
            Child = items,
        };
    }

    private Border RenderTablePlaceholder(string source)
    {
        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(8),
            Child = new TextBlock
            {
                Text = "Streaming table...",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.72,
            },
            Tag = source,
        };
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
            LinkInline link => this.RenderInline(link) + (string.IsNullOrWhiteSpace(link.Url) ? string.Empty : " (" + link.Url + ")"),
            Markdig.Extensions.Mathematics.MathInline math => "$" + math.Content + "$",
            HtmlInline html => html.Tag,
            _ => inline.ToString() ?? string.Empty,
        };
    }

    private SelectableTextBlock CreateSelectableTextBlock(string text)
    {
        return new SelectableTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private TextBlock CreateTextBlock(string text, FontWeight weight, double size)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = weight,
            FontSize = size,
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private double HeadingSize(int level)
    {
        return level switch
        {
            1 => 24,
            2 => 21,
            3 => 18,
            _ => 15,
        };
    }
}
