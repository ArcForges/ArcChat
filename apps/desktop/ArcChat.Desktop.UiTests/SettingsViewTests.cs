// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Settings;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class SettingsViewTests
{
    [Fact]
    public static void SettingsViewModelExposesCommandSurface()
    {
        SettingsViewModel viewModel = new SettingsViewModel();

        _ = viewModel.SaveCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ResetCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ImportCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ExportCommand.Should().BeAssignableTo<IRelayCommand>();
    }

    [Fact]
    public static async Task SettingsViewShowsRequiredSkeletonTabs()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                Window window = new Window
                {
                    Width = 720,
                    Height = 480,
                    Content = new SettingsView
                    {
                        DataContext = new SettingsViewModel(),
                    },
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    TabControl tabControl = window.GetVisualDescendants()
                        .OfType<TabControl>()
                        .Single(control => string.Equals(control.Name, "SettingsTabs", StringComparison.Ordinal));
                    string[] headers = tabControl.Items
                        .OfType<TabItem>()
                        .Select(tab => tab.Header?.ToString() ?? string.Empty)
                        .ToArray();

                    _ = headers.Should().Equal("General", "Appearance", "Locale");
                }
                finally
                {
                    window.Close();
                }
            },
            CancellationToken.None);
    }
}
