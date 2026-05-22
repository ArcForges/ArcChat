// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Input;

namespace ArcChat.Desktop.Shortcuts;

internal sealed record ShortcutDefinition
{
    public ShortcutDefinition(string action, string title, KeyGesture gesture, string source)
    {
        this.Action = action;
        this.Title = title;
        this.Gesture = gesture;
        this.Source = source;
    }

    public string Action { get; }

    public string Title { get; }

    public KeyGesture Gesture { get; }

    public string Source { get; }
}
