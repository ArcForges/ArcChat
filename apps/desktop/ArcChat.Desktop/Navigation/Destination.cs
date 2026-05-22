// Copyright (c) ArcForges. Licensed under the MIT License.

#pragma warning disable MA0048

using System.Diagnostics.CodeAnalysis;

namespace ArcChat.Desktop.Navigation;

public abstract record Destination
{
    protected Destination(string id, string title)
    {
        this.Id = id;
        this.Title = title;
    }

    public string Id { get; }

    public string Title { get; }
}

public sealed record Home : Destination
{
    public Home()
        : base("home", "Home")
    {
    }
}

public sealed record Chat : Destination
{
    public Chat(string conversationId)
        : base($"chat:{conversationId}", "Chat")
    {
        this.ConversationId = conversationId;
    }

    public string ConversationId { get; }
}

public sealed record Settings : Destination
{
    public Settings(SettingsSection? section = null)
        : base("settings", "Settings")
    {
        this.Section = section;
    }

    public SettingsSection? Section { get; }
}

public sealed record NewChat : Destination
{
    public NewChat()
        : base("new-chat", "New Chat")
    {
    }
}

public sealed record Masks : Destination
{
    public Masks()
        : base("masks", "Masks")
    {
    }
}

[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Matches the NextChat route name required by NC03.")]
public sealed record Plugins : Destination
{
    public Plugins()
        : base("plugins", "Plugins")
    {
    }
}

public sealed record Auth : Destination
{
    public Auth()
        : base("auth", "Auth")
    {
    }
}

public sealed record Sd : Destination
{
    public Sd()
        : base("sd", "Stable Diffusion")
    {
    }
}

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Matches the NextChat route name required by NC03.")]
public sealed record SdNew : Destination
{
    public SdNew()
        : base("sd-new", "New Image")
    {
    }
}

public sealed record Artifacts : Destination
{
    public Artifacts()
        : base("artifacts", "Artifacts")
    {
    }
}

public sealed record SearchChat : Destination
{
    public SearchChat()
        : base("search-chat", "Search Chat")
    {
    }
}

public sealed record McpMarket : Destination
{
    public McpMarket()
        : base("mcp-market", "MCP Market")
    {
    }
}

public enum SettingsSection
{
    General,
    Providers,
    Models,
    Shortcuts,
    Plugins,
}
