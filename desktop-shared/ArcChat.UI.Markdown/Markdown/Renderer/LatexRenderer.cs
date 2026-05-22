// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Media;

namespace ArcChat.UI.Markdown.Markdown.Renderer;

/// <summary>
/// Offline LaTeX renderer backed by the CSharpMath Avalonia MathView when available.
/// </summary>
public sealed class LatexRenderer
{
    private readonly string[] typeNames =
    {
        "CSharpMath.Avalonia.MathView, CSharpMath.Avalonia",
        "CSharpMath.Avalonia.MathView, Sylinko.CSharpMath.Avalonia",
    };

    /// <summary>Renders LaTeX source to an Avalonia control.</summary>
    public Control Render(string latex)
    {
        ArgumentNullException.ThrowIfNull(latex);
        if (TryCreateMathView(latex, this.typeNames) is { } mathView)
        {
            return mathView;
        }

        return new SelectableTextBlock
        {
            Text = latex,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
        };
    }

    private static Control? TryCreateMathView(string latex, IReadOnlyList<string> typeNames)
    {
        foreach (string typeName in typeNames)
        {
            Type? type = Type.GetType(typeName, throwOnError: false);
            if (type is null || Activator.CreateInstance(type) is not Control control)
            {
                continue;
            }

            type.GetProperty("LaTeX")?.SetValue(control, latex);
            return control;
        }

        return null;
    }
}
