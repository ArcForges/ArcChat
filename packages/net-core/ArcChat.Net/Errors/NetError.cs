// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Errors;

/// <summary>
/// Normalized network error payload used across providers.
/// </summary>
public abstract record NetError(string Message);

/// <summary>
/// Network connectivity failure.
/// </summary>
public sealed record NetworkError(string Message) : NetError(Message);

/// <summary>
/// Request timeout failure.
/// </summary>
public sealed record TimeoutError(string Message) : NetError(Message);

/// <summary>
/// Provider rate-limit failure.
/// </summary>
public sealed record RateLimitedError(string Message, TimeSpan? RetryAfter = null) : NetError(Message);

/// <summary>
/// Unauthorized failure.
/// </summary>
public sealed record UnauthorizedError(string Message) : NetError(Message);

/// <summary>
/// Forbidden failure.
/// </summary>
public sealed record ForbiddenError(string Message) : NetError(Message);

/// <summary>
/// Not-found failure.
/// </summary>
public sealed record NotFoundError(string Message) : NetError(Message);

/// <summary>
/// Bad-request failure.
/// </summary>
public sealed record BadRequestError(string Message) : NetError(Message);

/// <summary>
/// Server-side failure.
/// </summary>
public sealed record ServerError(string Message, int StatusCode) : NetError(Message);

/// <summary>
/// Cancellation failure.
/// </summary>
public sealed record CancelledError(string Message) : NetError(Message);

/// <summary>
/// Unknown network failure.
/// </summary>
public sealed record UnknownNetError(string Message) : NetError(Message);
