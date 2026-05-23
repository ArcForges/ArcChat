// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.GenericOpenAi;

/// <summary>
/// Configuration for <see cref="GenericOpenAiProvider"/>.
/// </summary>
public sealed class GenericOpenAiProviderOptions
{
    /// <summary>
    /// Gets the provider id stored in <see cref="ProviderConfig.Id"/>.
    /// </summary>
    public string ProviderConfigId { get; init; } = "custom-openai";

    /// <summary>
    /// Gets the provider name used by <see cref="ArcChat.ModelProviders.Core.ProviderId"/>.
    /// </summary>
    public string ProviderName { get; init; } = "GenericOpenAI";

    /// <summary>
    /// Gets the OpenAI-compatible base URI. Values ending in <c>/v1</c> are accepted.
    /// </summary>
    public Uri BaseUri { get; init; } = CreateBaseUri();

    /// <summary>
    /// Gets an optional resolved bearer API key.
    /// </summary>
    public string? ApiKey { get; init; } = Environment.GetEnvironmentVariable("GENERIC_OPENAI_API_KEY");

    /// <summary>
    /// Gets the unresolved API key reference stored in settings.
    /// </summary>
    public string? ApiKeyRef { get; init; } = Environment.GetEnvironmentVariable("GENERIC_OPENAI_API_KEY_REF");

    /// <summary>
    /// Gets a resolver that can dereference <see cref="ApiKeyRef"/> at call time.
    /// </summary>
    public Func<string, string?>? ApiKeyResolver { get; init; }

    /// <summary>
    /// Gets the configured offline model descriptors.
    /// </summary>
    public ImmutableArray<ModelDescriptor> Models { get; init; } = ImmutableArray<ModelDescriptor>.Empty;

    /// <summary>
    /// Gets a value indicating whether this endpoint supports OpenAI-compatible tools.
    /// </summary>
    public bool SupportsTools { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this endpoint supports OpenAI-compatible vision content parts.
    /// </summary>
    public bool SupportsVision { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this endpoint should be queried via <c>/v1/models</c>.
    /// </summary>
    public bool SupportsModelList { get; init; }

    /// <summary>
    /// Creates options from a persisted provider configuration.
    /// </summary>
    /// <param name="providerConfig">Persisted provider configuration.</param>
    /// <returns>Provider options mapped from settings.</returns>
    public static GenericOpenAiProviderOptions FromProviderConfig(ProviderConfig providerConfig)
    {
        return FromProviderConfigCore(providerConfig, null);
    }

    /// <summary>
    /// Creates options from a persisted provider configuration.
    /// </summary>
    /// <param name="providerConfig">Persisted provider configuration.</param>
    /// <param name="apiKeyResolver">API key reference resolver.</param>
    /// <returns>Provider options mapped from settings.</returns>
    public static GenericOpenAiProviderOptions FromProviderConfig(
        ProviderConfig providerConfig,
        Func<string, string?> apiKeyResolver)
    {
        ArgumentNullException.ThrowIfNull(apiKeyResolver);

        return FromProviderConfigCore(providerConfig, apiKeyResolver);
    }

    internal string? ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(this.ApiKey))
        {
            return this.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(this.ApiKeyRef))
        {
            return null;
        }

        string? resolved = this.ApiKeyResolver?.Invoke(this.ApiKeyRef);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        const string EnvironmentPrefix = "env:";
        if (this.ApiKeyRef.StartsWith(EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string environmentName = this.ApiKeyRef[EnvironmentPrefix.Length..];
            return string.IsNullOrWhiteSpace(environmentName)
                ? null
                : Environment.GetEnvironmentVariable(environmentName);
        }

        return null;
    }

    private static GenericOpenAiProviderOptions FromProviderConfigCore(
        ProviderConfig providerConfig,
        Func<string, string?>? apiKeyResolver)
    {
        ArgumentNullException.ThrowIfNull(providerConfig);

        bool supportsTools = ReadBool(providerConfig.Extra, "supportsTools", true);
        bool supportsVision = ReadBool(providerConfig.Extra, "supportsVision", true);
        bool supportsModelList = ReadBool(providerConfig.Extra, "supportsModelList", false);
        return new GenericOpenAiProviderOptions
        {
            ProviderConfigId = providerConfig.Id,
            ProviderName = providerConfig.ProviderName,
            BaseUri = providerConfig.BaseUrl ?? CreateBaseUri(),
            ApiKeyRef = providerConfig.ApiKeyRef,
            ApiKeyResolver = apiKeyResolver,
            Models = GenericOpenAiModelCatalog.Normalize(
                providerConfig.Models,
                providerConfig.Id,
                supportsTools,
                supportsVision),
            SupportsTools = supportsTools,
            SupportsVision = supportsVision,
            SupportsModelList = supportsModelList,
        };
    }

    private static bool ReadBool(ImmutableDictionary<string, JsonElement>? extra, string key, bool defaultValue)
    {
        if (extra is null || !extra.TryGetValue(key, out JsonElement value))
        {
            return defaultValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool parsed) => parsed,
            _ => defaultValue,
        };
    }

    private static Uri CreateBaseUri()
    {
        string? configured = Environment.GetEnvironmentVariable("GENERIC_OPENAI_BASE_URL");
        string value = string.IsNullOrWhiteSpace(configured) ? "http://localhost:8000" : configured;
        return new Uri(value, UriKind.Absolute);
    }
}
