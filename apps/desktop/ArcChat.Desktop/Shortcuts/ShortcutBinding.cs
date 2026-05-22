// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Input;

namespace ArcChat.Desktop.Shortcuts;

internal sealed record ShortcutBinding
{
    public ShortcutBinding(string action, string title, KeyGesture gesture, string gestureText)
    {
        this.Action = action;
        this.Title = title;
        this.Gesture = gesture;
        this.GestureText = gestureText;
    }

    public string Action { get; }

    public string Title { get; }

    public KeyGesture Gesture { get; }

    public string GestureText { get; }
}
