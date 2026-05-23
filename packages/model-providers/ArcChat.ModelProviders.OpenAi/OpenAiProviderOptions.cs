// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.OpenAi;

/// <summary>
/// Configuration for <see cref="OpenAiProvider"/>.
/// </summary>
public sealed class OpenAiProviderOptions
{
    /// <summary>
    /// Gets the OpenAI-compatible base URI. Defaults to <c>OPENAI_BASE_URL</c> or <c>https://api.openai.com</c>.
    /// </summary>
    public Uri BaseUri { get; init; } = CreateBaseUri();

    /// <summary>
    /// Gets the bearer API key. Defaults to <c>OPENAI_API_KEY</c>.
    /// </summary>
    public string? ApiKey { get; init; } = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    /// <summary>
    /// Gets the offline model descriptors exposed by this provider.
    /// </summary>
    public ImmutableArray<ModelDescriptor> Models { get; init; } = OpenAiModelCatalog.DefaultModels;

    private static Uri CreateBaseUri()
    {
        string? configured = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        string value = string.IsNullOrWhiteSpace(configured) ? "https://api.openai.com" : configured;
        return new Uri(value, UriKind.Absolute);
    }
}
