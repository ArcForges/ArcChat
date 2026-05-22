// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Net;
using ArcChat.Net.Sse;

namespace ArcChat.Net.Errors;

/// <summary>
/// Maps HTTP status codes and exceptions to NetError variants.
/// </summary>
public static class NetErrorMapper
{
    /// <summary>
    /// Maps an HTTP response to a normalized error.
    /// </summary>
    public static NetError FromResponse(HttpResponseMessage response, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        string text = message ?? response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestError(text),
            HttpStatusCode.Unauthorized => new UnauthorizedError(text),
            HttpStatusCode.Forbidden => new ForbiddenError(text),
            HttpStatusCode.NotFound => new NotFoundError(text),
            HttpStatusCode.TooManyRequests => new RateLimitedError(text, RetryAfterDelay.GetDelay(response.Headers)),
            >= HttpStatusCode.InternalServerError => new ServerError(text, (int)response.StatusCode),
            _ => new UnknownNetError(text),
        };
    }

    /// <summary>
    /// Maps a transport exception to a normalized error.
    /// </summary>
    public static NetError FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OperationCanceledException => new CancelledError(exception.Message),
            TimeoutException => new TimeoutError(exception.Message),
            HttpRequestException => new NetworkError(exception.Message),
            _ => new UnknownNetError(exception.Message),
        };
    }
}
