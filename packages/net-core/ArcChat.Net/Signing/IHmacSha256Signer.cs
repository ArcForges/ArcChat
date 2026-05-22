// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Signing;

/// <summary>
/// HMAC-SHA256 signer mapped from NextChat app/utils/hmac.ts.
/// </summary>
public interface IHmacSha256Signer
{
    /// <summary>
    /// Signs a payload with a binary key.
    /// </summary>
    byte[] Sign(byte[] key, byte[] payload);

    /// <summary>
    /// Signs a payload and returns lowercase hexadecimal output.
    /// </summary>
    string SignHex(byte[] key, byte[] payload);
}
