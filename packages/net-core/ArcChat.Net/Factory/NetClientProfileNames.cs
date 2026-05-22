// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Factory;

/// <summary>
/// Named HTTP profiles required by NC02.
/// </summary>
public static class NetClientProfileNames
{
    /// <summary>
    /// Default short-lived HTTP profile.
    /// </summary>
    public const string Default = "default";

    /// <summary>
    /// Streaming profile with long timeout and response-headers-read semantics.
    /// </summary>
    public const string Streaming = "streaming";

    /// <summary>
    /// Baidu signing profile.
    /// </summary>
    public const string SigningBaidu = "signing-baidu";

    /// <summary>
    /// Tencent signing profile.
    /// </summary>
    public const string SigningTencent = "signing-tencent";

    /// <summary>
    /// iFlytek signing profile.
    /// </summary>
    public const string SigningIflytek = "signing-iflytek";

    /// <summary>
    /// Azure AAD signing profile.
    /// </summary>
    public const string SigningAzureAad = "signing-azure-aad";
}
