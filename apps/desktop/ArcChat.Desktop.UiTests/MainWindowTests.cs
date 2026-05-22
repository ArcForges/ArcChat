// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.Desktop.Views;
using ArcChat.UI.Controls;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class MainWindowTests
{
    [Fact]
    public static async Task MainWindowDisplaysTwoPaneShell()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                AppNavigator navigator = new AppNavigator();
                MainWindowViewModel viewModel = new MainWindowViewModel(navigator);
                MainWindow window = new MainWindow()
                {
                    DataContext = viewModel,
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    TextBlock? textBlock = window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .SingleOrDefault(control => string.Equals(control.Name, "HeaderText", StringComparison.Ordinal));
                    _ = textBlock.Should().NotBeNull();
                    _ = textBlock!.Text.Should().Be("ArcChat");

                    Sidebar sidebar = window.GetVisualDescendants().OfType<Sidebar>().Single();
                    _ = sidebar.Items.Should().BeSameAs(viewModel.NavigationItems);

                    ContentControl content = window.GetVisualDescendants()
                        .OfType<ContentControl>()
                        .Single(control => string.Equals(control.Name, "ShellContent", StringComparison.Ordinal));
                    _ = content.Content.Should().BeOfType<Home>();
                }
                finally
                {
                    window.Close();
                    viewModel.Dispose();
                }
            },
            CancellationToken.None);
    }

    [Fact]
    public static async Task SidebarCommandDrivesShellContent()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                AppNavigator navigator = new AppNavigator();
                MainWindowViewModel viewModel = new MainWindowViewModel(navigator);
                MainWindow window = new MainWindow()
                {
                    DataContext = viewModel,
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    Button newChatButton = window.GetVisualDescendants()
                        .OfType<Button>()
                        .Single(button => Equals(button.CommandParameter, "new-chat"));
                    _ = newChatButton.Command.Should().NotBeNull();

                    newChatButton.Command!.Execute(newChatButton.CommandParameter);
                    Dispatcher.UIThread.RunJobs();

                    ContentControl content = window.GetVisualDescendants()
                        .OfType<ContentControl>()
                        .Single(control => string.Equals(control.Name, "ShellContent", StringComparison.Ordinal));
                    _ = content.Content.Should().BeOfType<NewChat>();
                }
                finally
                {
                    window.Close();
                    viewModel.Dispose();
                }
            },
            CancellationToken.None);
    }

    [Fact]
    public static async Task SplitterStateUpdatesSidebarNarrowMode()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                MainWindowViewModel viewModel = new MainWindowViewModel();
                MainWindow window = new MainWindow()
                {
                    DataContext = viewModel,
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    ResizableSplitPane splitPane = window.GetVisualDescendants().OfType<ResizableSplitPane>().Single();
                    Sidebar sidebar = window.GetVisualDescendants().OfType<Sidebar>().Single();

                    splitPane.PaneLength = new GridLength(ShellConstants.NarrowSidebarWidth);
                    Dispatcher.UIThread.RunJobs();

                    _ = viewModel.SidebarPaneLength.Value.Should().Be(ShellConstants.NarrowSidebarWidth);
                    _ = sidebar.IsNarrow.Should().BeTrue();
                }
                finally
                {
                    window.Close();
                    viewModel.Dispose();
                }
            },
            CancellationToken.None);
    }
}
