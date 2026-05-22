// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.UI.ArtifactViewer;

/// <summary>
/// Marker for the artifact viewer UI assembly until feature code lands in its owning step.
/// </summary>
internal static class ArtifactViewerMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
