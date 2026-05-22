// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Shortcuts;

internal sealed class ShortcutConflictException : InvalidOperationException
{
    public ShortcutConflictException()
        : this("Shortcut conflict.")
    {
    }

    public ShortcutConflictException(string message)
        : base(message)
    {
        this.Action = string.Empty;
        this.ExistingAction = string.Empty;
        this.Gesture = string.Empty;
    }

    public ShortcutConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.Action = string.Empty;
        this.ExistingAction = string.Empty;
        this.Gesture = string.Empty;
    }

    public ShortcutConflictException(string action, string existingAction, string gesture)
        : base($"Shortcut '{gesture}' for '{action}' conflicts with '{existingAction}'.")
    {
        this.Action = action;
        this.ExistingAction = existingAction;
        this.Gesture = gesture;
    }

    public string Action { get; }

    public string ExistingAction { get; }

    public string Gesture { get; }
}
