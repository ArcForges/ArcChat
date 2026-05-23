// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Google;

/// <summary>
/// Configuration for <see cref="GoogleProvider"/>.
/// </summary>
public sealed class GoogleProviderOptions
{
    /// <summary>
    /// Gets the Gemini base URI. Defaults to <c>GEMINI_BASE_URL</c> or <c>https://generativelanguage.googleapis.com</c>.
    /// </summary>
    public Uri BaseUri { get; init; } = CreateBaseUri();

    /// <summary>
    /// Gets the Google API key. Defaults to <c>GOOGLE_API_KEY</c>.
    /// </summary>
    public string? ApiKey { get; init; } = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

    /// <summary>
    /// Gets the safety threshold sent for each NextChat Gemini safety category.
    /// </summary>
    public GoogleSafetySettingsThreshold SafetyThreshold { get; init; } = GoogleSafetySettingsThreshold.BlockOnlyHigh;

    /// <summary>
    /// Gets the offline model descriptors exposed by this provider.
    /// </summary>
    public ImmutableArray<ModelDescriptor> Models { get; init; } = GoogleModelCatalog.DefaultModels;

    private static Uri CreateBaseUri()
    {
        string? configured = Environment.GetEnvironmentVariable("GEMINI_BASE_URL");
        string value = string.IsNullOrWhiteSpace(configured) ? "https://generativelanguage.googleapis.com" : configured;
        return new Uri(value, UriKind.Absolute);
    }
}
