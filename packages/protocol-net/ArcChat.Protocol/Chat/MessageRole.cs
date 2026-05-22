// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace ArcChat.Protocol.Chat;

/// <summary>
/// NextChat message roles from app/typing.ts and app/client/api.ts.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// System instruction message.
    /// </summary>
    [JsonStringEnumMemberName("system")]
    System,

    /// <summary>
    /// User-authored message.
    /// </summary>
    [JsonStringEnumMemberName("user")]
    User,

    /// <summary>
    /// Assistant-authored message.
    /// </summary>
    [JsonStringEnumMemberName("assistant")]
    Assistant,
}
