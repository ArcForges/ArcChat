// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Factory;

/// <summary>
/// Options for ArcChat.Net named client profiles.
/// </summary>
public sealed class NetCoreFactoryOptions
{
    /// <summary>
    /// Gets configured profiles.
    /// </summary>
    public IList<NetClientProfile> Profiles { get; } = new List<NetClientProfile>
    {
        new NetClientProfile(NetClientProfileNames.Default, TimeSpan.FromSeconds(100), HttpCompletionOption.ResponseContentRead),
        new NetClientProfile(NetClientProfileNames.Streaming, TimeSpan.FromMinutes(10), HttpCompletionOption.ResponseHeadersRead),
        new NetClientProfile(NetClientProfileNames.SigningBaidu, TimeSpan.FromSeconds(100), HttpCompletionOption.ResponseContentRead),
        new NetClientProfile(NetClientProfileNames.SigningTencent, TimeSpan.FromSeconds(100), HttpCompletionOption.ResponseContentRead),
        new NetClientProfile(NetClientProfileNames.SigningIflytek, TimeSpan.FromMinutes(10), HttpCompletionOption.ResponseHeadersRead),
        new NetClientProfile(NetClientProfileNames.SigningAzureAad, TimeSpan.FromSeconds(100), HttpCompletionOption.ResponseContentRead),
    };
}
