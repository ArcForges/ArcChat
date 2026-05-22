// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ArcChat.Net.Signing;

/// <summary>
/// Tencent TC3-HMAC-SHA256 signer mapped from app/utils/tencent.ts.
/// </summary>
public sealed class TencentTc3Signer
{
    private const string Algorithm = "TC3-HMAC-SHA256";
    private const string Endpoint = "hunyuan.tencentcloudapi.com";
    private const string Service = "hunyuan";
    private const string Action = "ChatCompletions";
    private const string Version = "2023-09-01";

    /// <summary>
    /// Creates signed Tencent headers for a JSON payload.
    /// </summary>
    public IReadOnlyDictionary<string, string> Sign(string payload, string secretId, string secretKey, DateTimeOffset timestamp)
    {
        long unix = timestamp.ToUnixTimeSeconds();
        string date = timestamp.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string hashedPayload = Sha256Hex(payload);
        string canonicalHeaders = "content-type:application/json\nhost:" + Endpoint + "\nx-tc-action:" + Action.ToLowerInvariant() + "\n";
        string signedHeaders = "content-type;host;x-tc-action";
        string canonicalRequest = string.Join(
            '\n',
            "POST",
            "/",
            string.Empty,
            canonicalHeaders,
            signedHeaders,
            hashedPayload);
        string credentialScope = date + "/" + Service + "/tc3_request";
        string stringToSign = string.Join('\n', Algorithm, unix.ToString(CultureInfo.InvariantCulture), credentialScope, Sha256Hex(canonicalRequest));
        byte[] kDate = Hmac(Encoding.UTF8.GetBytes("TC3" + secretKey), date);
        byte[] kService = Hmac(kDate, Service);
        byte[] kSigning = Hmac(kService, "tc3_request");
        string signature = Convert.ToHexString(Hmac(kSigning, stringToSign)).ToLowerInvariant();
        string authorization = $"{Algorithm} Credential={secretId}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Authorization"] = authorization,
            ["Content-Type"] = "application/json",
            ["Host"] = Endpoint,
            ["X-TC-Action"] = Action,
            ["X-TC-Timestamp"] = unix.ToString(CultureInfo.InvariantCulture),
            ["X-TC-Version"] = Version,
            ["X-TC-Region"] = string.Empty,
        };
    }

    private static byte[] Hmac(byte[] key, string data)
    {
        using HMACSHA256 hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Sha256Hex(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
