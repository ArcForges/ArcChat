// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Controls.Tests;

public sealed class SidebarKeyboardTests
{
    [Fact]
    public static async Task SidebarItemsExposeKeyboardFocusOrder()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(ControlTestAppBuilder));
        await session.Dispatch(
            () =>
            {
                Sidebar sidebar = new Sidebar()
                {
                    Items = new SidebarItem[]
                    {
                        new SidebarItem("home", "Home", "H"),
                        new SidebarItem("settings", "Settings", "S"),
                        new SidebarItem("new-chat", "New Chat", "+", true),
                    },
                };
                Window window = new Window()
                {
                    Width = 360,
                    Height = 420,
                    Content = sidebar,
                };

                try
                {
                    window.Show();
                    window.Activate();
                    Dispatcher.UIThread.RunJobs();

                    Button[] buttons = sidebar.GetVisualDescendants().OfType<Button>().ToArray();
                    _ = buttons.Should().HaveCount(3);
                    foreach (Button button in buttons)
                    {
                        _ = button.Focusable.Should().BeTrue();
                    }

                    _ = buttons.Select(ReadTitle).Should().Equal("Home", "Settings", "New Chat");
                }
                finally
                {
                    window.Close();
                }
            },
            CancellationToken.None);
    }

    private static string? ReadTitle(Button button)
    {
        return button.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text) && text.Length > 1);
    }
}
