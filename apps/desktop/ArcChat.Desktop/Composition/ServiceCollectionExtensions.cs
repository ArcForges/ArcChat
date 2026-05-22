// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.Shortcuts;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence;
using Microsoft.Extensions.DependencyInjection;
using ObservableSettingsRepository = ArcChat.LocalServices.Settings.SettingsRepository;
using PersistenceSettingsRepository = ArcChat.LocalPersistence.Repositories.ISettingsRepository;
using SettingsRepository = ArcChat.LocalServices.Settings.ISettingsRepository;

namespace ArcChat.Desktop.Composition;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddArcChatDesktop(this IServiceCollection services)
    {
        _ = services.AddSingleton(_ => CreateDatabase());
        _ = services.AddSingleton<PersistenceSettingsRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Settings);
        _ = services.AddSingleton<SettingsRepository, ObservableSettingsRepository>();
        _ = services.AddSingleton<IAppNavigator, AppNavigator>();
        _ = services.AddSingleton<IShortcutRegistry, ShortcutRegistry>();
        _ = services.AddSingleton<IThemeService, AvaloniaThemeService>();
        _ = services.AddSingleton<ILocaleService>(
            _ => LocaleService.FromDirectory(
                Path.Combine(AppContext.BaseDirectory, "Resources", "Locales"),
                CultureInfo.CurrentUICulture.Name));
        _ = services.AddTransient(provider => new SettingsViewModel(
            provider.GetRequiredService<SettingsRepository>(),
            provider.GetRequiredService<ILocaleService>(),
            provider.GetRequiredService<IThemeService>()));
        _ = services.AddTransient(provider => new MainWindowViewModel(
            provider.GetRequiredService<IAppNavigator>(),
            provider.GetRequiredService<SettingsViewModel>(),
            provider.GetRequiredService<CommandPaletteViewModel>(),
            provider.GetRequiredService<ILocaleService>()));
        _ = services.AddTransient(provider => new CommandPaletteViewModel(provider.GetRequiredService<IShortcutRegistry>()));
        return services;
    }

    private static ArcChatDatabase CreateDatabase()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = AppContext.BaseDirectory;
        }

        string directory = Path.Combine(root, "ArcChat");
        Directory.CreateDirectory(directory);
        ArcChatDatabase database = new ArcChatDatabase(Path.Combine(directory, "ArcChat.db"));
        database.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        return database;
    }
}
