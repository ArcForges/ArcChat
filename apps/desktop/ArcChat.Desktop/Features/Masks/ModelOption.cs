// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Masks;

internal sealed class ModelOption
{
    public ModelOption(string id, string displayName)
    {
        this.Id = id;
        this.DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public override string ToString()
    {
        return this.DisplayName;
    }
}
