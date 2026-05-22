// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.Contracts;

namespace ArcChat.Agent;

/// <summary>
/// Marker for the agent runtime assembly until feature code lands in its owning step.
/// </summary>
internal static class AgentMarker
{
    /// <summary>
    /// Returns true when the assembly is loaded and linked into the solution.
    /// </summary>
    [Pure]
    internal static bool IsAvailable() => true;
}
