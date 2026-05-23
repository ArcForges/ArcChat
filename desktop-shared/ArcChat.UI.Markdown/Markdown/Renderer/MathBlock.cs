// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Media;

namespace ArcChat.UI.Markdown.Markdown.Renderer;

/// <summary>
/// Display math block.
/// </summary>
public sealed class MathBlock : Border
{
    /// <summary>Initializes a new instance of the <see cref="MathBlock"/> class.</summary>
    public MathBlock(string source, bool isPlaceholder, LatexRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(renderer);
        this.Source = TrimMathDelimiters(source);
        this.IsPlaceholder = isPlaceholder;
        this.CornerRadius = new Avalonia.CornerRadius(6);
        this.Padding = new Avalonia.Thickness(12);
        this.BorderBrush = Brushes.LightGray;
        this.BorderThickness = new Avalonia.Thickness(1);
        this.Child = isPlaceholder
            ? new TextBlock { Text = "Streaming math block...", Opacity = 0.72 }
            : renderer.Render(this.Source);
    }

    /// <summary>Gets the LaTeX source.</summary>
    public string Source { get; }

    /// <summary>Gets a value indicating whether this block represents an incomplete stream.</summary>
    public bool IsPlaceholder { get; }

    private static string TrimMathDelimiters(string source)
    {
        string trimmed = source.Trim();
        if (trimmed.StartsWith("$$", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..];
        }

        if (trimmed.EndsWith("$$", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^2];
        }

        return trimmed.Trim();
    }
}
