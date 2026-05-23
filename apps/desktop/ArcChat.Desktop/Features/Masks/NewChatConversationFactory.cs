// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Providers;

namespace ArcChat.Desktop.Features.Masks;

internal static class NewChatConversationFactory
{
    internal static ModelConfig CreateActiveModelConfig(ModelConfig baseModelConfig, string? providerName, string? modelId)
    {
        return baseModelConfig with
        {
            Model = modelId ?? ModelConfig.NextChatDefault.Model,
            ProviderName = providerName ?? ModelConfig.NextChatDefault.ProviderName,
        };
    }

    internal static Mask CreateBlankMask(string conversationId, long now, ModelConfig modelConfig)
    {
        return new Mask(
            "mask-" + conversationId,
            now,
            "1f603",
            "Default",
            false,
            ImmutableArray<Message>.Empty,
            true,
            modelConfig,
            "en",
            false,
            ImmutableArray<string>.Empty);
    }

    internal static Mask ApplyActiveProvider(Mask recommendedMask, ModelConfig activeModelConfig, long now)
    {
        return recommendedMask with
        {
            CreatedAt = now,
            ModelConfig = recommendedMask.ModelConfig with
            {
                ProviderName = activeModelConfig.ProviderName,
                CompressProviderName = activeModelConfig.CompressProviderName,
            },
        };
    }
}
