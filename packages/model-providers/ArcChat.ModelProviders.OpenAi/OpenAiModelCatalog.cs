// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.OpenAi;

internal static class OpenAiModelCatalog
{
    public static ImmutableArray<ModelDescriptor> DefaultModels { get; } = CreateDefaultModels();

    private static ImmutableArray<ModelDescriptor> CreateDefaultModels()
    {
        string[] modelIds =
        [
            "gpt-4o-mini",
            "gpt-4o",
            "gpt-4.1",
            "gpt-4.1-mini",
            "gpt-4.1-nano",
            "gpt-5",
            "gpt-5-mini",
            "gpt-5-nano",
            "gpt-4-turbo",
            "gpt-3.5-turbo",
            "o1-mini",
            "o3-mini",
            "o3",
            "o4-mini",
        ];

        ImmutableArray<ModelDescriptor>.Builder builder = ImmutableArray.CreateBuilder<ModelDescriptor>(modelIds.Length);
        for (int index = 0; index < modelIds.Length; index++)
        {
            string modelId = modelIds[index];
            builder.Add(
                new ModelDescriptor(
                    modelId,
                    modelId,
                    "openai",
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
        if (OpenAiProvider.IsVisionModel(modelId))
        {
            capabilities.Add(new VisionCapability());
        }

        if (OpenAiProvider.IsReasoningModel(modelId))
        {
            capabilities.Add(new ReasoningCapability());
        }

        return capabilities.ToImmutable();
    }
}
