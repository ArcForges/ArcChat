// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Theme;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace ArcChat.UI.Controls.Tests;

internal sealed class ControlTestApplication : Application
{
    public override void Initialize()
    {
        this.Styles.Add(new ArcChatTheme());
        this.Styles.Add(new StyleInclude(new Uri("avares://ArcChat.UI.Controls/"))
        {
            Source = new Uri("avares://ArcChat.UI.Controls/Themes/Generic.axaml"),
        });
    }
}
