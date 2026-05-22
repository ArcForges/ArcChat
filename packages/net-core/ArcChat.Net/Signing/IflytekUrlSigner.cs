// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ArcChat.Net.Signing;

/// <summary>
/// iFlytek signed WSS URL builder.
/// </summary>
public sealed class IflytekUrlSigner
{
    /// <summary>
    /// Builds a signed iFlytek WebSocket URL.
    /// </summary>
    public Uri Sign(Uri baseUri, string apiKey, string apiSecret, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        string host = baseUri.Host;
        string path = string.IsNullOrEmpty(baseUri.PathAndQuery) ? "/" : baseUri.PathAndQuery;
        string date = timestamp.UtcDateTime.ToString("r", CultureInfo.InvariantCulture);
        string signatureOrigin = "host: " + host + "\n" + "date: " + date + "\n" + "GET " + path + " HTTP/1.1";
        string signature;
        using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret)))
        {
            signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureOrigin)));
        }

        string authorizationOrigin = $"api_key=\"{apiKey}\", algorithm=\"hmac-sha256\", headers=\"host date request-line\", signature=\"{signature}\"";
        string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationOrigin));
        string separator = baseUri.Query.Length == 0 ? "?" : "&";
        string signed = baseUri + separator + "authorization=" + Uri.EscapeDataString(authorization) + "&date=" + Uri.EscapeDataString(date) + "&host=" + Uri.EscapeDataString(host);
        return new Uri(signed);
    }
}
