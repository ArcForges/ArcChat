// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.UI.Theme;

/// <summary>
/// Marker for the Avalonia theme resources assembly until feature code lands in its owning step.
/// </summary>
internal static class ThemeMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
