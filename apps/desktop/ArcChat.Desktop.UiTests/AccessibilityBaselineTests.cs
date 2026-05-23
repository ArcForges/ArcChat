// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.Desktop.Views;
using ArcChat.UI.Controls;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class AccessibilityBaselineTests
{
    [Fact]
    public static async Task ShellInteractiveControlsExposeAutomationNamesAndTabTraversal()
    {
        using HeadlessUnitTestSession session = TestAppBuilder.StartHeadlessSession();
        await session.Dispatch(
            () =>
            {
                using MainWindowViewModel viewModel = new MainWindowViewModel(new AppNavigator());
                MainWindow window = new MainWindow
                {
                    DataContext = viewModel,
                };

                try
                {
                    window.Show();
                    window.Activate();
                    Dispatcher.UIThread.RunJobs();

                    AssertNamedInteractiveControls(window);
                    AssertTabVisitsVisibleControls(window);
                }
                finally
                {
                    TestAppBuilder.CloseWindow(window);
                }
            },
            CancellationToken.None);
    }

    [Fact]
    public static async Task SettingsInteractiveControlsExposeAutomationNamesAndTabTraversal()
    {
        using HeadlessUnitTestSession session = TestAppBuilder.StartHeadlessSession();
        await session.Dispatch(
            () =>
            {
                using SettingsViewModel viewModel = new SettingsViewModel();
                SettingsView settingsView = new SettingsView
                {
                    DataContext = viewModel,
                };
                Window window = new Window
                {
                    Width = 720,
                    Height = 480,
                    Content = settingsView,
                };

                try
                {
                    window.Show();
                    window.Activate();
                    Dispatcher.UIThread.RunJobs();

                    TabItem[] tabs = window.GetVisualDescendants().OfType<TabItem>().ToArray();
                    foreach (TabItem tab in tabs)
                    {
                        tab.IsSelected = true;
                        Dispatcher.UIThread.RunJobs();

                        AssertNamedInteractiveControls(window);
                        AssertTabVisitsVisibleControls(window);
                    }
                }
                finally
                {
                    TestAppBuilder.CloseWindow(window);
                }
            },
            CancellationToken.None);
    }

    private static void AssertNamedInteractiveControls(TopLevel topLevel)
    {
        string[] missingNames = FindInteractiveControls(topLevel)
            .Where(static control => string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
            .Select(Describe)
            .ToArray();

        _ = missingNames.Should().BeEmpty("every tab-reachable interactive control must expose an automation name");
    }

    private static void AssertTabVisitsVisibleControls(Window window)
    {
        Control[] interactiveControls = FindInteractiveControls(window);
        string[] expectedNames = interactiveControls
            .Select(AutomationProperties.GetName)
            .OfType<string>()
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _ = expectedNames.Should().NotBeEmpty();

        Control first = interactiveControls[0];
        _ = first.Focus(NavigationMethod.Tab, KeyModifiers.None);
        Dispatcher.UIThread.RunJobs();

        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < expectedNames.Length * 4; index++)
        {
            if (window.FocusManager?.GetFocusedElement() is IInputElement focused
                && TryReadAutomationName(focused, out string? name))
            {
                _ = visited.Add(name);
            }

            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
            Dispatcher.UIThread.RunJobs();
        }

        foreach (string expectedName in expectedNames)
        {
            _ = visited.Should().Contain(expectedName);
        }
    }

    private static Control[] FindInteractiveControls(TopLevel topLevel)
    {
        return topLevel.GetVisualDescendants()
            .OfType<Control>()
            .Where(IsUserFacingInteractiveControl)
            .Where(static control => control.IsVisible && control.Focusable && control.Bounds.Width > 0 && control.Bounds.Height > 0)
            .ToArray();
    }

    private static bool IsUserFacingInteractiveControl(Control control)
    {
        if (control.Name?.StartsWith("PART_", StringComparison.Ordinal) == true)
        {
            return false;
        }

        if (control is TabItem tabItem && !tabItem.IsSelected)
        {
            return false;
        }

        return control is Button
            or CheckBox
            or ComboBox
            or GridSplitter
            or IconButton
            or NumericUpDown
            or TabItem
            or TextBox;
    }

    private static bool TryReadAutomationName(IInputElement inputElement, out string name)
    {
        if (inputElement is Control control)
        {
            string? automationName = SelfAndAncestors(control)
                .Select(AutomationProperties.GetName)
                .Where(static candidateName => !string.IsNullOrWhiteSpace(candidateName))
                .FirstOrDefault();
            if (automationName is not null)
            {
                name = automationName;
                return true;
            }
        }

        name = string.Empty;
        return false;
    }

    private static IEnumerable<Control> SelfAndAncestors(Control control)
    {
        yield return control;

        foreach (Control ancestor in control.GetVisualAncestors().OfType<Control>())
        {
            yield return ancestor;
        }
    }

    private static string Describe(Control control)
    {
        return string.IsNullOrWhiteSpace(control.Name)
            ? control.GetType().Name
            : control.GetType().Name + "#" + control.Name;
    }
}
