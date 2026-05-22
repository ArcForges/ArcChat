// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// Common settings projections used by the desktop shell.
/// </summary>
public static class SettingsKeys
{
    /// <summary>
    /// UI theme setting.
    /// </summary>
    public static KeyExpression<string> Theme { get; } = new KeyExpression<string>("ui.theme", snapshot => snapshot.Ui.Theme);

    /// <summary>
    /// UI font size setting.
    /// </summary>
    public static KeyExpression<int> FontSize { get; } = new KeyExpression<int>("ui.fontSize", snapshot => snapshot.Ui.FontSize);

    /// <summary>
    /// UI font family setting.
    /// </summary>
    public static KeyExpression<string> FontFamily { get; } = new KeyExpression<string>("ui.fontFamily", snapshot => snapshot.Ui.FontFamily);
}
