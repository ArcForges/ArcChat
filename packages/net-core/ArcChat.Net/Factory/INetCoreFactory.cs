// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Factory;

/// <summary>
/// Owns configured HttpClient access for provider, sync, and integration code.
/// </summary>
public interface INetCoreFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets a named configured client.
    /// </summary>
    /// <param name="name">Client profile name.</param>
    /// <returns>A cached HttpClient for the requested profile.</returns>
    HttpClient GetClient(string name);

    /// <summary>
    /// Gets metadata for a named client profile.
    /// </summary>
    /// <param name="name">Client profile name.</param>
    /// <returns>The configured profile.</returns>
    NetClientProfile GetProfile(string name);
}
