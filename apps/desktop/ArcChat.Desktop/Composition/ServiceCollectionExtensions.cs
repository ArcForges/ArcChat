// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.Shortcuts;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence;
using ArcChat.LocalPersistence.Repositories;
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
        _ = services.AddSingleton<IConversationRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Conversations);
        _ = services.AddSingleton<PersistenceSettingsRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Settings);
        _ = services.AddSingleton<SettingsRepository, ObservableSettingsRepository>();
        _ = services.AddSingleton<IAppNavigator, AppNavigator>();
        _ = services.AddSingleton<IShortcutRegistry, ShortcutRegistry>();
        _ = services.AddSingleton<IThemeService, AvaloniaThemeService>();
        string localeBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        string localeDirectory = Path.Join(localeBaseDirectory, "Resources", "Locales");
        _ = services.AddSingleton<ILocaleService>(
            _ => LocaleService.FromDirectory(
                localeDirectory,
                CultureInfo.CurrentUICulture.Name));
        _ = services.AddTransient(provider => new SettingsViewModel(
            provider.GetRequiredService<SettingsRepository>(),
            provider.GetRequiredService<ILocaleService>(),
            provider.GetRequiredService<IThemeService>()));
        _ = services.AddTransient(
            provider =>
            {
                ConversationListViewModel viewModel = new ConversationListViewModel(
                    provider.GetRequiredService<IConversationRepository>());
                viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                return viewModel;
            });
        _ = services.AddTransient(provider => new MainWindowViewModel(
            provider.GetRequiredService<IAppNavigator>(),
            provider.GetRequiredService<ConversationListViewModel>(),
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

        string directory = Path.Join(root, "ArcChat");
        Directory.CreateDirectory(directory);
        ArcChatDatabase database = new ArcChatDatabase(Path.Join(directory, "ArcChat.db"));
        database.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        return database;
    }
}
