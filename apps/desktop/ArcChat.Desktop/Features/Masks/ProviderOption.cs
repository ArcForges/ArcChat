// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;

namespace ArcChat.Desktop.Features.Masks;

internal sealed class ProviderOption
{
    public ProviderOption(string id, string providerName, string displayName, ImmutableArray<ModelOption> models)
    {
        this.Id = id;
        this.ProviderName = providerName;
        this.DisplayName = displayName;
        this.Models = models;
    }

    public string Id { get; }

    public string ProviderName { get; }

    public string DisplayName { get; }

    public ImmutableArray<ModelOption> Models { get; }

    public override string ToString()
    {
        return this.DisplayName;
    }
}
