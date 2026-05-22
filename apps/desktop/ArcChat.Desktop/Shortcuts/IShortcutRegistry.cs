// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Input;

namespace ArcChat.Desktop.Shortcuts;

internal interface IShortcutRegistry
{
    IReadOnlyList<ShortcutBinding> Bindings { get; }

    void Register(string action, KeyGesture gesture);
}
