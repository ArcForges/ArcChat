// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Anthropic;

/// <summary>
/// Configuration for <see cref="AnthropicProvider"/>.
/// </summary>
public sealed class AnthropicProviderOptions
{
    /// <summary>
    /// Gets the Anthropic base URI. Defaults to <c>ANTHROPIC_BASE_URL</c> or <c>https://api.anthropic.com</c>.
    /// </summary>
    public Uri BaseUri { get; init; } = CreateBaseUri();

    /// <summary>
    /// Gets the Anthropic API key. Defaults to <c>ANTHROPIC_API_KEY</c>.
    /// </summary>
    public string? ApiKey { get; init; } = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    /// <summary>
    /// Gets the Anthropic API version header value.
    /// </summary>
    public string ApiVersion { get; init; } = Environment.GetEnvironmentVariable("ANTHROPIC_API_VERSION") ?? "2023-06-01";

    /// <summary>
    /// Gets the offline model descriptors exposed by this provider.
    /// </summary>
    public ImmutableArray<ModelDescriptor> Models { get; init; } = AnthropicModelCatalog.DefaultModels;

    private static Uri CreateBaseUri()
    {
        string? configured = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
        string value = string.IsNullOrWhiteSpace(configured) ? "https://api.anthropic.com" : configured;
        return new Uri(value, UriKind.Absolute);
    }
}
