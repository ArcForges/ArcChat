// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace ArcChat.Net.Signing;

/// <summary>
/// Baidu OAuth token response from app/utils/baidu.ts.
/// </summary>
public sealed record BaiduIamToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("error")] int? Error = null);

/// <summary>
/// Source-generated JSON metadata for Baidu IAM token responses.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BaiduIamToken))]
public partial class BaiduIamTokenJsonContext : JsonSerializerContext
{
}
