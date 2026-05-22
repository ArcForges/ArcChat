// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
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
        _ = services.AddSingleton<ILocaleService>(
            _ => LocaleService.FromDirectory(
                Path.Combine(AppContext.BaseDirectory, "Resources", "Locales"),
                CultureInfo.CurrentUICulture.Name));
        _ = services.AddTransient(provider => new SettingsViewModel(
            provider.GetRequiredService<SettingsRepository>(),
            provider.GetRequiredService<ILocaleService>()));
        _ = services.AddTransient(provider => new MainWindowViewModel(
            provider.GetRequiredService<IAppNavigator>(),
            provider.GetRequiredService<SettingsViewModel>()));
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
