// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Theme;
using Avalonia;

namespace ArcChat.UI.Markdown.Tests;

internal sealed class MarkdownTestApplication : Application
{
    public override void Initialize()
    {
        this.Styles.Add(new ArcChatTheme());
    }
}
