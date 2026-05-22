// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Factory;

/// <summary>
/// HTTP client profile metadata.
/// </summary>
public sealed record NetClientProfile(
    string Name,
    TimeSpan Timeout,
    HttpCompletionOption CompletionOption);
