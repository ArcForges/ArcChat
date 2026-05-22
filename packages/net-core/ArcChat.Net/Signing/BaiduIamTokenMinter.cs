// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace ArcChat.Net.Signing;

/// <summary>
/// Baidu IAM token minter mapped from app/utils/baidu.ts.
/// </summary>
public sealed class BaiduIamTokenMinter
{
    private readonly HttpClient httpClient;
    private readonly Uri oauthEndpoint;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CachedToken> cache = new ConcurrentDictionary<string, CachedToken>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduIamTokenMinter"/> class.
    /// </summary>
    public BaiduIamTokenMinter(HttpClient httpClient, Uri oauthEndpoint, TimeProvider? timeProvider = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.oauthEndpoint = oauthEndpoint ?? throw new ArgumentNullException(nameof(oauthEndpoint));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets a cached or newly minted Baidu IAM token.
    /// </summary>
    public async Task<BaiduIamToken> GetTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        string key = clientId + "\n" + clientSecret;
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        if (this.cache.TryGetValue(key, out CachedToken? cached) && cached.ExpiresAt - TimeSpan.FromMinutes(5) > now)
        {
            return cached.Token;
        }

        Uri requestUri = new Uri(this.oauthEndpoint, $"?grant_type=client_credentials&client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}");
        using HttpResponseMessage response = await this.httpClient.PostAsync(requestUri, null, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();
        BaiduIamToken? token = await response.Content.ReadFromJsonAsync(BaiduIamTokenJsonContext.Default.BaiduIamToken, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            throw new InvalidOperationException("Baidu token response was empty.");
        }

        _ = this.cache.TryAdd(key, new CachedToken(token, now + TimeSpan.FromSeconds(token.ExpiresIn)));
        return token;
    }

    private sealed record CachedToken(BaiduIamToken Token, DateTimeOffset ExpiresAt);
}
