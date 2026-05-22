// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.UI.Controls;

/// <summary>
/// Marker for the shared Avalonia controls assembly until feature code lands in its owning step.
/// </summary>
internal static class ControlsMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
