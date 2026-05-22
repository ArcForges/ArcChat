// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net;
using ArcChat.Net.Errors;
using ArcChat.Net.Resilience;
using ArcChat.Net.Sse;
using FluentAssertions;
using Xunit;

namespace ArcChat.Net.Tests;

public sealed class ResilienceAndErrorTests
{
    [Fact]
    public void RetryAfterUsesSecondsOrHttpDate()
    {
        using HttpResponseMessage seconds = new(HttpStatusCode.TooManyRequests);
        seconds.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(3));

        DateTimeOffset future = DateTimeOffset.UtcNow.AddSeconds(2);
        using HttpResponseMessage date = new(HttpStatusCode.ServiceUnavailable);
        date.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(future);

        _ = RetryAfterDelay.GetDelay(seconds.Headers).Should().Be(TimeSpan.FromSeconds(3));
        _ = RetryAfterDelay.GetDelay(date.Headers).Should().BePositive();
    }

    [Fact]
    public async Task AwaitingTokenBucketQueuesInsteadOfDroppingCallers()
    {
        await using AwaitingTokenBucket bucket = new(1, 1, TimeSpan.FromMilliseconds(50));

        await bucket.WaitAsync(1, CancellationToken.None);
        Func<Task> wait = async () => await bucket.WaitAsync(1, CancellationToken.None);

        await wait.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NetErrorMapperNormalizesHttpAndTransportErrors()
    {
        using HttpResponseMessage unauthorized = new(HttpStatusCode.Unauthorized);
        using HttpResponseMessage rateLimited = new(HttpStatusCode.TooManyRequests);
        using HttpResponseMessage serverError = new(HttpStatusCode.InternalServerError);
        rateLimited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));

        _ = NetErrorMapper.FromResponse(unauthorized).Should().BeOfType<UnauthorizedError>();
        _ = NetErrorMapper.FromResponse(rateLimited).Should().BeEquivalentTo(new RateLimitedError("Too Many Requests", TimeSpan.FromSeconds(7)));
        _ = NetErrorMapper.FromResponse(serverError).Should().BeOfType<ServerError>();
        _ = NetErrorMapper.FromException(new TimeoutException("slow")).Should().BeOfType<TimeoutError>();
        _ = NetErrorMapper.FromException(new HttpRequestException("offline")).Should().BeOfType<NetworkError>();
    }
}
