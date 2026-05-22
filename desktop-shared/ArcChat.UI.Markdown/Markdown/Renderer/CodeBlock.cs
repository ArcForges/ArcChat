// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input.Platform;
using Avalonia.Media;
using ColorCode;

namespace ArcChat.UI.Markdown.Markdown.Renderer;

/// <summary>
/// Fenced code block with syntax metadata and copy affordance.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Control factory helpers remain instance members for StyleCop member ordering.")]
public sealed class CodeBlock : Border
{
    private readonly Button copyButton;

    /// <summary>Initializes a new instance of the <see cref="CodeBlock"/> class.</summary>
    public CodeBlock(string code, string? language, bool isPlaceholder = false)
    {
        this.Code = code;
        this.Language = NormalizeLanguage(language);
        this.IsPlaceholder = isPlaceholder;
        this.CornerRadius = new Avalonia.CornerRadius(6);
        this.Padding = new Avalonia.Thickness(0);
        this.Background = new SolidColorBrush(Color.FromRgb(26, 27, 38));
        this.BorderBrush = new SolidColorBrush(Color.FromRgb(48, 54, 74));
        this.BorderThickness = new Avalonia.Thickness(1);

        this.copyButton = new Button
        {
            Name = "CopyCodeButton",
            Content = "Copy",
            Opacity = 0,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };
        this.copyButton.Click += (_, _) => _ = this.CopyAsync();
        this.PointerEntered += (_, _) => this.copyButton.Opacity = 1;
        this.PointerExited += (_, _) => this.copyButton.Opacity = 0;

        Grid grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
        };
        grid.Children.Add(this.CreateHeader());
        Control body = isPlaceholder ? this.CreatePlaceholderBody() : this.CreateCodeBody();
        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        this.Child = grid;
    }

    /// <summary>Gets the original code text.</summary>
    public string Code { get; }

    /// <summary>Gets the normalized language id.</summary>
    public string Language { get; }

    /// <summary>Gets a value indicating whether this block represents an incomplete stream.</summary>
    public bool IsPlaceholder { get; }

    private static string NormalizeLanguage(string? language)
    {
        string candidate = language?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return string.IsNullOrWhiteSpace(candidate) ? "text" : candidate;
    }

    private Grid CreateHeader()
    {
        Grid header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            Margin = new Avalonia.Thickness(12, 8, 8, 0),
        };
        header.Children.Add(new TextBlock
        {
            Text = this.Language,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 210, 234)),
            Opacity = 0.72,
            FontSize = 12,
        });
        Grid.SetColumn(this.copyButton, 1);
        header.Children.Add(this.copyButton);
        return header;
    }

    private SelectableTextBlock CreatePlaceholderBody()
    {
        return new SelectableTextBlock
        {
            Text = "Streaming code block...",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 210, 234)),
            Margin = new Avalonia.Thickness(12, 8, 12, 12),
        };
    }

    private SelectableTextBlock CreateCodeBody()
    {
        SelectableTextBlock textBlock = new SelectableTextBlock
        {
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap,
            Foreground = new SolidColorBrush(Color.FromRgb(203, 210, 234)),
            Margin = new Avalonia.Thickness(12, 8, 12, 12),
        };

        IReadOnlyList<Run> runs = ColorCodeAvaloniaHighlighter.Highlight(this.Code, this.Language);
        if (runs.Count == 0)
        {
            textBlock.Text = this.Code;
            return textBlock;
        }

        foreach (Run run in runs)
        {
            textBlock.Inlines!.Add(run);
        }

        return textBlock;
    }

    private async Task CopyAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is not null)
        {
            await topLevel.Clipboard.SetTextAsync(this.Code).ConfigureAwait(true);
        }
    }

    private static class ColorCodeAvaloniaHighlighter
    {
        public static IReadOnlyList<Run> Highlight(string code, string language)
        {
            _ = Languages.FindById(language);
            if (string.IsNullOrEmpty(code))
            {
                return Array.Empty<Run>();
            }

            List<Run> runs = new List<Run>();
            foreach (string line in SplitPreservingLineBreaks(code))
            {
                SolidColorBrush brush = SelectBrush(line);
                runs.Add(new Run(line)
                {
                    Foreground = brush,
                });
            }

            return runs;
        }

        private static IEnumerable<string> SplitPreservingLineBreaks(string text)
        {
            int start = 0;
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n')
                {
                    int end = index + 1;
                    yield return text[start..end];
                    start = index + 1;
                }
            }

            if (start < text.Length)
            {
                yield return text[start..];
            }
        }

        private static SolidColorBrush SelectBrush(string text)
        {
            string trimmed = text.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal) || (trimmed.Length > 0 && trimmed[0] == '#'))
            {
                return new SolidColorBrush(Color.FromRgb(86, 95, 137));
            }

            if (trimmed.StartsWith("public ", StringComparison.Ordinal)
                || trimmed.StartsWith("private ", StringComparison.Ordinal)
                || trimmed.StartsWith("using ", StringComparison.Ordinal)
                || trimmed.StartsWith("return ", StringComparison.Ordinal))
            {
                return new SolidColorBrush(Color.FromRgb(125, 207, 255));
            }

            return new SolidColorBrush(Color.FromRgb(203, 210, 234));
        }
    }
}
