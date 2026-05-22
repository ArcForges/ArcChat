// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.ModelProviders.GenericOpenAi;

/// <summary>
/// Marker for the generic OpenAI-compatible provider assembly until feature code lands in its owning step.
/// </summary>
internal static class GenericOpenAiMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
