// Copyright (c) ArcForges. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Controls.Tests;

public sealed class ControlSnapshotTests
{
    [Fact]
    public static async Task ControlTemplateSnapshotsMatch()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(ControlTestAppBuilder));
        await session.Dispatch(
            () =>
            {
                foreach ((string name, string expected) in ControlCases())
                {
                    Control control = CreateControl(name);
                    Window window = new Window()
                    {
                        Width = 640,
                        Height = 420,
                        Content = control,
                    };

                    try
                    {
                        window.Show();
                        Dispatcher.UIThread.RunJobs();

                        string snapshot = $"{name}:{control.GetType().Name}:{control.Bounds.Width:0}x{control.Bounds.Height:0}";
                        _ = snapshot.Should().Be(expected);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            },
            CancellationToken.None);
    }

    public static IEnumerable<(string Name, string Expected)> ControlCases()
    {
        return new (string Name, string Expected)[]
        {
            ("IconButton", "IconButton:IconButton:640x420"),
            ("TextField", "TextField:TextField:640x420"),
            ("ConfirmDialog", "ConfirmDialog:ConfirmDialog:640x420"),
            ("LoadingSpinner", "LoadingSpinner:LoadingSpinner:640x420"),
            ("EmptyState", "EmptyState:EmptyState:640x420"),
            ("ResizableSplitPane", "ResizableSplitPane:ResizableSplitPane:640x420"),
            ("Sidebar", "Sidebar:Sidebar:640x420"),
        };
    }

    private static Control CreateControl(string name)
    {
        return name switch
        {
            "IconButton" => new IconButton { Text = "New", Icon = "+" },
            "TextField" => new TextField { Text = "ArcChat", Watermark = "Search" },
            "ConfirmDialog" => new ConfirmDialog { Title = "Delete", Message = "Confirm?", ConfirmText = "Yes", CancelText = "No" },
            "LoadingSpinner" => new LoadingSpinner { Message = "Loading" },
            "EmptyState" => new EmptyState { Title = "Empty", Message = "No chats", ActionText = "Start" },
            "ResizableSplitPane" => new ResizableSplitPane { Left = new TextBlock { Text = "Left" }, Right = new TextBlock { Text = "Right" } },
            "Sidebar" => CreateSidebar(),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown control snapshot case."),
        };
    }

    private static Sidebar CreateSidebar()
    {
        return new Sidebar
        {
            Items = new SidebarItem[]
            {
                new SidebarItem("home", "Home", "H"),
                new SidebarItem("settings", "Settings", "S"),
            },
        };
    }
}
