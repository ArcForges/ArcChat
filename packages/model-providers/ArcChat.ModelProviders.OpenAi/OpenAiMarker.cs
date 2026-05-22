// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.ModelProviders.OpenAi;

/// <summary>
/// Marker for the OpenAI provider assembly until feature code lands in its owning step.
/// </summary>
internal static class OpenAiMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
