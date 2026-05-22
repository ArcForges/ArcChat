// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Media;

namespace ArcChat.UI.Theme;

/// <summary>
/// WCAG contrast calculator for NC03 accessibility color-token checks.
/// </summary>
public static class ColorContrastCalculator
{
    /// <summary>
    /// WCAG AA contrast ratio required for normal text.
    /// </summary>
    public const double NormalTextAaRatio = 4.5;

    /// <summary>
    /// Computes the WCAG contrast ratio between two colors.
    /// </summary>
    /// <param name="foreground">Foreground text color.</param>
    /// <param name="background">Background color.</param>
    /// <returns>Contrast ratio.</returns>
    public static double ContrastRatio(Color foreground, Color background)
    {
        double foregroundLuminance = RelativeLuminance(foreground);
        double backgroundLuminance = RelativeLuminance(background);
        double lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        double darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        return (0.2126 * Linearize(color.R)) + (0.7152 * Linearize(color.G)) + (0.0722 * Linearize(color.B));
    }

    private static double Linearize(byte value)
    {
        double channel = value / 255d;
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
