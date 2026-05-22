// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Text;
using ArcChat.Net.Signing;
using FluentAssertions;
using Xunit;

namespace ArcChat.Net.Tests;

public sealed class SigningTests
{
    [Fact]
    public void HmacSha256MatchesNextChatUtilityVector()
    {
        HmacSha256Signer signer = new();

        string hex = signer.SignHex(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes("payload"));

        _ = hex.Should().Be("b82fcb791acec57859b989b430a826488ce2e479fdf92326bd0a2e8375a42ba4");
    }

    [Fact]
    public void TencentTc3ProducesStableHeaders()
    {
        TencentTc3Signer signer = new();
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000);

        IReadOnlyDictionary<string, string> headers = signer.Sign("{\"model\":\"hunyuan\"}", "secret-id", "secret-key", timestamp);

        _ = headers["X-TC-Timestamp"].Should().Be("1700000000");
        _ = headers["Authorization"].Should().StartWith("TC3-HMAC-SHA256 Credential=secret-id/2023-11-14/hunyuan/tc3_request");
        _ = headers["Authorization"].Should().Contain("SignedHeaders=content-type;host;x-tc-action");
        _ = headers["Authorization"].Should().Contain("Signature=");
    }

    [Fact]
    public async Task BaiduTokenMinterCachesWithFiveMinuteJitter()
    {
        CountingHandler handler = new("{\"access_token\":\"token-1\",\"expires_in\":3600}");
        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("https://example.invalid"),
        };
        BaiduIamTokenMinter minter = new(client, new Uri("https://example.invalid/oauth/2.0/token"));

        BaiduIamToken first = await minter.GetTokenAsync("ak", "sk", CancellationToken.None);
        BaiduIamToken second = await minter.GetTokenAsync("ak", "sk", CancellationToken.None);

        _ = first.AccessToken.Should().Be("token-1");
        _ = second.AccessToken.Should().Be("token-1");
        _ = handler.Count.Should().Be(1);
    }

    [Fact]
    public void IflytekSignerBuildsSignedWssUrl()
    {
        IflytekUrlSigner signer = new();
        Uri signed = signer.Sign(
            new Uri("wss://spark-api.xf-yun.com/v1/chat"),
            "api-key",
            "api-secret",
            DateTimeOffset.Parse("2026-05-22T17:00:00Z", CultureInfo.InvariantCulture));

        _ = signed.Query.Should().Contain("authorization=");
        _ = signed.Query.Should().Contain("date=Fri%2C%2022%20May%202026%2017%3A00%3A00%20GMT");
        _ = signed.Query.Should().Contain("host=spark-api.xf-yun.com");
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly string body;

        public CountingHandler(string body)
        {
            this.body = body;
        }

        public int Count { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
