// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArcChat.UI.Markdown.Markdown.Renderer;

/// <summary>
/// Default-build Mermaid diagram placeholder.
/// </summary>
public sealed class MermaidPlaceholder : Border
{
    /// <summary>Initializes a new instance of the <see cref="MermaidPlaceholder"/> class.</summary>
    public MermaidPlaceholder(string source, bool isPartial)
    {
        this.Source = source;
        this.IsPartial = isPartial;
        this.SourceHash = CreateHash(source);
        this.CornerRadius = new Avalonia.CornerRadius(6);
        this.Padding = new Avalonia.Thickness(12);
        this.BorderBrush = Brushes.LightGray;
        this.BorderThickness = new Avalonia.Thickness(1);
        this.Child = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = isPartial ? "Streaming Mermaid diagram..." : "Mermaid diagram placeholder",
                    FontWeight = FontWeight.SemiBold,
                },
                new SelectableTextBlock
                {
                    Text = source,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
                    Opacity = 0.72,
                },
            },
        };
    }

    /// <summary>Gets the Mermaid source.</summary>
    public string Source { get; }

    /// <summary>Gets a value indicating whether this placeholder represents an incomplete stream.</summary>
    public bool IsPartial { get; }

    /// <summary>Gets the deterministic cache key for a future WebView renderer.</summary>
    public string SourceHash { get; }

    private static string CreateHash(string source)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash);
    }
}
