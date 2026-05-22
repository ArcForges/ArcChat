// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Controls;

/// <summary>
/// Navigation item rendered by the NC03 Sidebar control.
/// </summary>
public sealed record SidebarItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SidebarItem"/> class.
    /// </summary>
    /// <param name="id">Stable destination or action id.</param>
    /// <param name="title">Localized display title.</param>
    /// <param name="glyph">Short glyph used when the sidebar is narrow.</param>
    /// <param name="isPrimary">Whether the item should use the primary action style.</param>
    public SidebarItem(string id, string title, string glyph, bool isPrimary = false)
    {
        this.Id = id;
        this.Title = title;
        this.Glyph = glyph;
        this.IsPrimary = isPrimary;
    }

    /// <summary>Gets the stable destination or action id.</summary>
    public string Id { get; }

    /// <summary>Gets the localized display title.</summary>
    public string Title { get; }

    /// <summary>Gets the short glyph used when the sidebar is narrow.</summary>
    public string Glyph { get; }

    /// <summary>Gets a value indicating whether the item should use the primary action style.</summary>
    public bool IsPrimary { get; }
}
