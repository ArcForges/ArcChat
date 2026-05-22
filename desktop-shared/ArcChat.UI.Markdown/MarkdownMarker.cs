// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.UI.Markdown;

/// <summary>
/// Marker for the streaming Markdown UI assembly until feature code lands in its owning step.
/// </summary>
internal static class MarkdownMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
