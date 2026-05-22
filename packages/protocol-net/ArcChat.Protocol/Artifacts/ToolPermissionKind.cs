// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Protocol.Artifacts;

/// <summary>
/// Tool permission level used by the later tool runtime.
/// </summary>
public enum ToolPermissionKind
{
    /// <summary>
    /// Read-only tool.
    /// </summary>
    Read,

    /// <summary>
    /// Tool may write local or remote state.
    /// </summary>
    Write,

    /// <summary>
    /// Tool requires explicit confirmation before every call.
    /// </summary>
    ConfirmEachCall,
}
