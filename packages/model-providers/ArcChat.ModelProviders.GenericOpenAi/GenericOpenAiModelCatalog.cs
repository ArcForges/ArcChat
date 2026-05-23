// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.GenericOpenAi;

internal static class GenericOpenAiModelCatalog
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex[] VisionModelRegexes =
    [
        CreateRegex("vision"),
        CreateRegex("gpt-4o"),
        CreateRegex("gpt-4\\.1"),
        CreateRegex("claude.*[34]"),
        CreateRegex("gemini-1\\.5"),
        CreateRegex("gemini-exp"),
        CreateRegex("gemini-2\\.[05]"),
        CreateRegex("learnlm"),
        CreateRegex("qwen-vl"),
        CreateRegex("qwen2-vl"),
        CreateRegex("gpt-4-turbo(?!.*preview)"),
        CreateRegex("^dall-e-3$"),
        CreateRegex("glm-4v"),
        CreateRegex("vl", RegexOptions.IgnoreCase),
        CreateRegex("o3"),
        CreateRegex("o4-mini"),
        CreateRegex("grok-4", RegexOptions.IgnoreCase),
        CreateRegex("gpt-5"),
    ];

    private static readonly Regex[] ExcludedVisionModelRegexes =
    [
        CreateRegex("claude-3-5-haiku-20241022"),
    ];

    public static ImmutableArray<ModelDescriptor> Normalize(
        ImmutableArray<ModelDescriptor> models,
        string providerId,
        bool supportsTools,
        bool supportsVision)
    {
        if (models.IsDefaultOrEmpty)
        {
            return ImmutableArray<ModelDescriptor>.Empty;
        }

        ImmutableArray<ModelDescriptor>.Builder builder = ImmutableArray.CreateBuilder<ModelDescriptor>(models.Length);
        foreach (ModelDescriptor model in models)
        {
            builder.Add(model with
            {
                ProviderId = string.IsNullOrWhiteSpace(model.ProviderId) ? providerId : model.ProviderId,
                Capabilities = CreateCapabilities(model.Id, supportsTools, supportsVision),
            });
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<ModelDescriptor> FromModelIds(
        IEnumerable<string> modelIds,
        string providerId,
        bool supportsTools,
        bool supportsVision)
    {
        ArgumentNullException.ThrowIfNull(modelIds);

        ImmutableArray<ModelDescriptor>.Builder builder = ImmutableArray.CreateBuilder<ModelDescriptor>();
        int sorted = -1000;
        foreach (string modelId in modelIds.Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            builder.Add(new ModelDescriptor(
                modelId,
                modelId,
                providerId,
                true,
                sorted++,
                CreateCapabilities(modelId, supportsTools, supportsVision)));
        }

        return builder.ToImmutable();
    }

    public static bool IsReasoningModel(string modelId)
    {
        return modelId.StartsWith("o1", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o3", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("o4-mini", StringComparison.OrdinalIgnoreCase)
            || modelId.StartsWith("gpt-5", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsVisionModel(string modelId)
    {
        return !ExcludedVisionModelRegexes.Any(regex => regex.IsMatch(modelId))
            && VisionModelRegexes.Any(regex => regex.IsMatch(modelId));
    }

    private static ImmutableArray<ProviderCapability> CreateCapabilities(
        string modelId,
        bool supportsTools,
        bool supportsVision)
    {
        ImmutableArray<ProviderCapability>.Builder capabilities = ImmutableArray.CreateBuilder<ProviderCapability>();
        capabilities.Add(new StreamingCapability());
        if (supportsTools)
        {
            capabilities.Add(new ToolsCapability());
        }

        if (supportsVision && IsVisionModel(modelId))
        {
            capabilities.Add(new VisionCapability());
        }

        if (IsReasoningModel(modelId))
        {
            capabilities.Add(new ReasoningCapability());
        }

        return capabilities.ToImmutable();
    }

    private static Regex CreateRegex(string pattern, RegexOptions options = RegexOptions.None)
    {
        return new Regex(pattern, options | RegexOptions.CultureInvariant, RegexTimeout);
    }
}
