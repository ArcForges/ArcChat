// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Localization;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
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
        using SettingsViewModel viewModel = new SettingsViewModel();

        _ = viewModel.SaveCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ResetCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ImportCommand.Should().BeAssignableTo<IRelayCommand>();
        _ = viewModel.ExportCommand.Should().BeAssignableTo<IRelayCommand>();
    }

    [Fact]
    public static void ThemeSelectionAppliesImmediately()
    {
        RecordingThemeService themeService = new RecordingThemeService();
        using SettingsViewModel viewModel = new SettingsViewModel(
            new InMemorySettingsRepository(SettingsDefaults.Create()),
            null,
            themeService);

        viewModel.Theme = "dark";

        _ = themeService.AppliedThemes.Should().EndWith("dark");
    }

    [Fact]
    public static void LocaleSelectionUpdatesSettingsStringsWithoutRestart()
    {
        LocaleService localeService = CreateLocaleService();
        using SettingsViewModel viewModel = new SettingsViewModel(
            new InMemorySettingsRepository(SettingsDefaults.Create()),
            localeService);

        viewModel.CurrentLocale = "fr";

        _ = viewModel.SettingsTitle.Should().Be("Parametres");
        _ = viewModel.ThemeLabel.Should().Be("Theme FR");
        _ = viewModel.CurrentLocale.Should().Be("fr");
    }

    [Fact]
    public static async Task SettingsViewShowsRequiredSkeletonTabs()
    {
        using HeadlessUnitTestSession session = TestAppBuilder.StartHeadlessSession();
        await session.Dispatch(
            () =>
            {
                using SettingsViewModel viewModel = new SettingsViewModel();
                Window window = new Window
                {
                    Width = 720,
                    Height = 480,
                    Content = new SettingsView
                    {
                        DataContext = viewModel,
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
                    TestAppBuilder.CloseWindow(window);
                }
            },
            CancellationToken.None);
    }

    private static LocaleService CreateLocaleService()
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> locales =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Settings.Title"] = "Settings",
                    ["Settings.Theme"] = "Theme",
                    ["Settings.Lang.Name"] = "Language",
                },
                ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Settings.Title"] = "Parametres",
                    ["Settings.Theme"] = "Theme FR",
                    ["Settings.Lang.Name"] = "Langue",
                },
            };

        return new LocaleService(locales, "en");
    }

    private sealed class RecordingThemeService : IThemeService
    {
        public List<string> AppliedThemes { get; } = new List<string>();

        public void Apply(string theme)
        {
            this.AppliedThemes.Add(theme);
        }
    }

    private sealed class InMemorySettingsRepository : ISettingsRepository
    {
        private SettingsSnapshot snapshot;

        public InMemorySettingsRepository(SettingsSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.snapshot);
        }

        public Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            this.snapshot = snapshot;
            return Task.CompletedTask;
        }

        public IObservable<T> Observe<T>(KeyExpression<T> keyExpression)
        {
            return new SingleValueObservable<T>(keyExpression.Evaluate(this.snapshot));
        }
    }

    private sealed class SingleValueObservable<T> : IObservable<T>
    {
        private readonly T value;

        public SingleValueObservable(T value)
        {
            this.value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(this.value);
            return new Subscription();
        }
    }

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
