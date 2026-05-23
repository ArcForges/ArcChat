// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Google;

internal static class GoogleModelCatalog
{
    public static ImmutableArray<ModelDescriptor> DefaultModels { get; } = CreateDefaultModels();

    private static ImmutableArray<ModelDescriptor> CreateDefaultModels()
    {
        string[] modelIds =
        [
            "gemini-1.5-pro-latest",
            "gemini-1.5-pro",
            "gemini-1.5-pro-002",
            "gemini-1.5-flash-latest",
            "gemini-1.5-flash-8b-latest",
            "gemini-1.5-flash",
            "gemini-1.5-flash-8b",
            "gemini-1.5-flash-002",
            "learnlm-1.5-pro-experimental",
            "gemini-exp-1206",
            "gemini-2.0-flash",
            "gemini-2.0-flash-exp",
            "gemini-2.0-flash-lite-preview-02-05",
            "gemini-2.0-flash-thinking-exp",
            "gemini-2.0-flash-thinking-exp-1219",
            "gemini-2.0-flash-thinking-exp-01-21",
            "gemini-2.0-pro-exp",
            "gemini-2.0-pro-exp-02-05",
            "gemini-2.5-pro-preview-06-05",
            "gemini-2.5-pro",
        ];

        ImmutableArray<ModelDescriptor>.Builder builder = ImmutableArray.CreateBuilder<ModelDescriptor>(modelIds.Length);
        for (int index = 0; index < modelIds.Length; index++)
        {
            string modelId = modelIds[index];
            builder.Add(
                new ModelDescriptor(
                    modelId,
                    modelId,
                    "google",
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
        if (GoogleProvider.IsVisionModel(modelId))
        {
            capabilities.Add(new VisionCapability());
        }

        if (GoogleProvider.IsReasoningModel(modelId))
        {
            capabilities.Add(new ReasoningCapability());
        }

        return capabilities.ToImmutable();
    }
}
