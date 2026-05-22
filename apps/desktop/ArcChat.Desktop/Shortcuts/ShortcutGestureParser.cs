// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Input;

namespace ArcChat.Desktop.Shortcuts;

internal static class ShortcutGestureParser
{
    internal static KeyGesture Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        string[] tokens = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException("Shortcut gesture is empty.");
        }

        KeyModifiers modifiers = KeyModifiers.None;
        Key? key = null;
        foreach (string token in tokens)
        {
            if (TryParseModifier(token, ref modifiers))
            {
                continue;
            }

            if (key is not null)
            {
                throw new FormatException("Shortcut gesture can contain only one non-modifier key.");
            }

            key = ParseKey(token);
        }

        return key is null
            ? throw new FormatException("Shortcut gesture must contain a key.")
            : new KeyGesture(key.Value, modifiers);
    }

    internal static string Format(KeyGesture gesture)
    {
        List<string> parts = new List<string>();
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            parts.Add("Meta");
        }

        parts.Add(FormatKey(gesture.Key));
        return string.Join("+", parts);
    }

    private static bool TryParseModifier(string token, ref KeyModifiers modifiers)
    {
        switch (Normalize(token))
        {
            case "CTRL":
            case "CONTROL":
                modifiers |= KeyModifiers.Control;
                return true;
            case "ALT":
            case "OPTION":
                modifiers |= KeyModifiers.Alt;
                return true;
            case "SHIFT":
                modifiers |= KeyModifiers.Shift;
                return true;
            case "CMD":
            case "COMMAND":
            case "META":
            case "⌘":
                modifiers |= KeyModifiers.Meta;
                return true;
            default:
                return false;
        }
    }

    private static Key ParseKey(string token)
    {
        return Normalize(token) switch
        {
            "ESC" or "ESCAPE" => Key.Escape,
            "BACKSPACE" or "BACK" => Key.Back,
            "ARROWUP" or "UP" => Key.Up,
            "ARROWDOWN" or "DOWN" => Key.Down,
            ";" or "SEMICOLON" => Key.OemSemicolon,
            "/" or "SLASH" => Key.OemQuestion,
            _ => Enum.TryParse(token, ignoreCase: true, out Key parsed)
                ? parsed
                : throw new FormatException("Unknown shortcut key '" + token + "'."),
        };
    }

    private static string FormatKey(Key key)
    {
        return key switch
        {
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.Up => "ArrowUp",
            Key.Down => "ArrowDown",
            Key.OemSemicolon => ";",
            Key.OemQuestion => "/",
            _ => key.ToString(),
        };
    }

    private static string Normalize(string token)
    {
        return token.Trim().ToUpperInvariant();
    }
}
