// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.ModelProviders.Ai302;

/// <summary>
/// Marker for the 302.AI provider assembly until feature code lands in its owning step.
/// </summary>
internal static class Ai302Marker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
