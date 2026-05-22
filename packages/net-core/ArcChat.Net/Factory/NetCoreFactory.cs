// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace ArcChat.Net.Factory;

/// <summary>
/// Cached named HttpClient factory backed by Microsoft.Extensions.Http.
/// </summary>
public sealed class NetCoreFactory : INetCoreFactory
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly Dictionary<string, NetClientProfile> profiles;
    private readonly ConcurrentDictionary<string, HttpClient> clients = new ConcurrentDictionary<string, HttpClient>(StringComparer.Ordinal);
    private int disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreFactory"/> class.
    /// </summary>
    public NetCoreFactory(IHttpClientFactory httpClientFactory, IOptions<NetCoreFactoryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.profiles = options.Value.Profiles.ToDictionary(profile => profile.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public HttpClient GetClient(string name)
    {
        ObjectDisposedException.ThrowIf(this.disposed != 0, this);
        return this.clients.GetOrAdd(name, this.httpClientFactory.CreateClient);
    }

    /// <inheritdoc />
    public NetClientProfile GetProfile(string name)
    {
        if (this.profiles.TryGetValue(name, out NetClientProfile? profile))
        {
            return profile;
        }

        throw new InvalidOperationException($"HTTP profile '{name}' is not configured.");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        foreach (HttpClient client in this.clients.Values)
        {
            client.Dispose();
        }

        this.clients.Clear();
        return ValueTask.CompletedTask;
    }
}
