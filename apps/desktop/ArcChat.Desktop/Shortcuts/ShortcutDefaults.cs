// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Shortcuts;

internal static class ShortcutDefaults
{
    internal const string CommandPaletteOpen = "command.palette.open";
    internal const string ChatNew = "chat.new";
    internal const string ChatFocusInput = "chat.focusInput";
    internal const string ChatCopyLastCode = "chat.copyLastCode";
    internal const string ChatCopyLastMessage = "chat.copyLastMessage";
    internal const string ChatShowShortcuts = "chat.showShortcuts";
    internal const string ChatClearContext = "chat.clearContext";
    internal const string ChatPrevious = "chat.previous";
    internal const string ChatNext = "chat.next";

    internal static IReadOnlyList<ShortcutDefinition> All { get; } = new[]
    {
        Create(CommandPaletteOpen, "Open command palette", "Ctrl+K", "ArcChat NC03.05"),
        Create(ChatNew, "New chat", "Ctrl+Shift+O", "NextChat/app/components/chat.tsx"),
        Create(ChatFocusInput, "Focus chat input", "Shift+Esc", "NextChat/app/components/chat.tsx"),
        Create(ChatCopyLastCode, "Copy last code block", "Ctrl+Shift+;", "NextChat/app/components/chat.tsx"),
        Create(ChatCopyLastMessage, "Copy last reply", "Ctrl+Shift+C", "NextChat/app/components/chat.tsx"),
        Create(ChatShowShortcuts, "Show shortcuts", "Ctrl+/", "NextChat/app/components/chat.tsx"),
        Create(ChatClearContext, "Clear context", "Ctrl+Shift+Backspace", "NextChat/app/components/chat.tsx"),
        Create(ChatPrevious, "Previous chat", "Ctrl+ArrowUp", "NextChat/app/components/sidebar.tsx"),
        Create(ChatPrevious, "Previous chat", "Alt+ArrowUp", "NextChat/app/components/sidebar.tsx"),
        Create(ChatNext, "Next chat", "Ctrl+ArrowDown", "NextChat/app/components/sidebar.tsx"),
        Create(ChatNext, "Next chat", "Alt+ArrowDown", "NextChat/app/components/sidebar.tsx"),
    };

    private static ShortcutDefinition Create(string action, string title, string gesture, string source)
    {
        return new ShortcutDefinition(action, title, ShortcutGestureParser.Parse(gesture), source);
    }
}
