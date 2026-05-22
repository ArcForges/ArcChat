// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Features.Shell;

public sealed record CommandPaletteItem
{
    public CommandPaletteItem(string action, string title, string gestureText)
    {
        this.Action = action;
        this.Title = title;
        this.GestureText = gestureText;
    }

    public string Action { get; }

    public string Title { get; }

    public string GestureText { get; }
}
