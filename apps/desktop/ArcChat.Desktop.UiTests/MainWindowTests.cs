// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.Desktop.Views;
using ArcChat.UI.Controls;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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
                using MainWindowViewModel viewModel = new MainWindowViewModel(navigator);
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
                    DestinationPlaceholderViewModel placeholder = content.Content.Should().BeOfType<DestinationPlaceholderViewModel>().Subject;
                    _ = placeholder.Id.Should().Be("home");
                }
                finally
                {
                    window.Close();
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
                using MainWindowViewModel viewModel = new MainWindowViewModel(navigator);
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
                    DestinationPlaceholderViewModel placeholder = content.Content.Should().BeOfType<DestinationPlaceholderViewModel>().Subject;
                    _ = placeholder.Id.Should().Be("new-chat");
                }
                finally
                {
                    window.Close();
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
                using MainWindowViewModel viewModel = new MainWindowViewModel();
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
                }
            },
            CancellationToken.None);
    }

    [Fact]
    public static async Task MainWindowRegistersCommandPaletteShortcut()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                using MainWindowViewModel viewModel = new MainWindowViewModel();
                MainWindow window = new MainWindow()
                {
                    DataContext = viewModel,
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    KeyBinding keyBinding = window.KeyBindings
                        .OfType<KeyBinding>()
                        .Single(binding => binding.Gesture?.Key == Key.K
                            && binding.Gesture.KeyModifiers == KeyModifiers.Control);

                    _ = keyBinding.Command.Should().BeSameAs(viewModel.CommandPalette.OpenCommand);
                    keyBinding.Command!.Execute(null);

                    _ = viewModel.CommandPalette.IsOpen.Should().BeTrue();
                    _ = viewModel.CommandPalette.Items.Should().Contain(item => item.GestureText == "Ctrl+K");
                }
                finally
                {
                    window.Close();
                }
            },
            CancellationToken.None);
    }

    [Fact]
    public static void LocaleSelectionUpdatesSidebarStringsWithoutRestart()
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> locales =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Settings.Title"] = "Settings",
                    ["Chat.InputActions.Masks"] = "Masks",
                },
                ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Settings.Title"] = "Parametres",
                    ["Chat.InputActions.Masks"] = "Masques",
                },
            };
        LocaleService localeService = new LocaleService(locales, "en");
        using SettingsViewModel settingsViewModel = new SettingsViewModel();
        using MainWindowViewModel viewModel = new MainWindowViewModel(
            new AppNavigator(),
            settingsViewModel,
            new CommandPaletteViewModel(),
            localeService);

        localeService.SetCulture("fr");

        _ = viewModel.NavigationItems.Single(item => string.Equals(item.Id, "settings", StringComparison.Ordinal))
            .Title.Should().Be("Parametres");
        _ = viewModel.NavigationItems.Single(item => string.Equals(item.Id, "masks", StringComparison.Ordinal))
            .Title.Should().Be("Masques");
    }
}
