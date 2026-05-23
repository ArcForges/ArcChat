// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Anthropic;

internal static class AnthropicModelCatalog
{
    public static ImmutableArray<ModelDescriptor> DefaultModels { get; } = CreateDefaultModels();

    private static ImmutableArray<ModelDescriptor> CreateDefaultModels()
    {
        string[] modelIds =
        [
            "claude-instant-1.2",
            "claude-2.0",
            "claude-2.1",
            "claude-3-sonnet-20240229",
            "claude-3-opus-20240229",
            "claude-3-opus-latest",
            "claude-3-haiku-20240307",
            "claude-3-5-haiku-20241022",
            "claude-3-5-haiku-latest",
            "claude-3-5-sonnet-20240620",
            "claude-3-5-sonnet-20241022",
            "claude-3-5-sonnet-latest",
            "claude-3-7-sonnet-20250219",
            "claude-3-7-sonnet-latest",
            "claude-sonnet-4-20250514",
            "claude-opus-4-20250514",
        ];

        ImmutableArray<ModelDescriptor>.Builder builder = ImmutableArray.CreateBuilder<ModelDescriptor>(modelIds.Length);
        for (int index = 0; index < modelIds.Length; index++)
        {
            string modelId = modelIds[index];
            builder.Add(
                new ModelDescriptor(
                    modelId,
                    modelId,
                    "anthropic",
                    true,
                    index,
                    GetCapabilities(modelId)));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ProviderCapability> GetCapabilities(string modelId)
    {
        ImmutableArray<ProviderCapability>.Builder capabilities = ImmutableArray.CreateBuilder<ProviderCapability>();
        capabilities.Add(new StreamingCapability());
        capabilities.Add(new ToolsCapability());
        if (AnthropicProvider.IsVisionModel(modelId))
        {
            capabilities.Add(new VisionCapability());
        }

        if (AnthropicProvider.IsReasoningModel(modelId))
        {
            capabilities.Add(new ReasoningCapability());
        }

        return capabilities.ToImmutable();
    }
}
