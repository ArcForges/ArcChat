// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.ViewModels;

namespace ArcChat.Desktop.Features.Shell;

internal sealed class DestinationPlaceholderViewModel : ViewModelBase
{
    public DestinationPlaceholderViewModel(string title, string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        this.Title = title;
        this.Id = id;
    }

    public string Title { get; }

    public string Id { get; }
}
