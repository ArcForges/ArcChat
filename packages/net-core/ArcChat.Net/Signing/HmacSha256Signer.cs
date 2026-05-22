// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Security.Cryptography;

namespace ArcChat.Net.Signing;

/// <summary>
/// BCL-backed HMAC-SHA256 signer.
/// </summary>
public sealed class HmacSha256Signer : IHmacSha256Signer
{
    /// <inheritdoc />
    public byte[] Sign(byte[] key, byte[] payload)
    {
        using HMACSHA256 hmac = new HMACSHA256(key);
        return hmac.ComputeHash(payload);
    }

    /// <inheritdoc />
    public string SignHex(byte[] key, byte[] payload)
    {
        return Convert.ToHexString(this.Sign(key, payload)).ToLowerInvariant();
    }
}
