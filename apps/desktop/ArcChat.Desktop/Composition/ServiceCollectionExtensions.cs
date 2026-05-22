// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Agent;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.Shortcuts;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence;
using ArcChat.LocalPersistence.Repositories;
using ArcChat.ModelProviders.Core;
using ArcChat.Net.Factory;
using Microsoft.Extensions.DependencyInjection;
using ObservableSettingsRepository = ArcChat.LocalServices.Settings.SettingsRepository;
using PersistenceSettingsRepository = ArcChat.LocalPersistence.Repositories.ISettingsRepository;
using SettingsRepository = ArcChat.LocalServices.Settings.ISettingsRepository;

namespace ArcChat.Desktop.Composition;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddArcChatDesktop(this IServiceCollection services)
    {
        _ = services.AddArcChatNetCore();
        AddCoreServices(services);
        AddFeatureViewModels(services);
        return services;
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        _ = services.AddSingleton(_ => CreateDatabase());
        _ = services.AddSingleton<IConversationRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Conversations);
        _ = services.AddSingleton<IMessageRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Messages);
        _ = services.AddSingleton<PersistenceSettingsRepository>(provider => provider.GetRequiredService<ArcChatDatabase>().Settings);
        _ = services.AddSingleton<IChatProviderRegistry>(_ => ModelProviderCoreDefaults.CreateRegistry());
        _ = services.AddSingleton<IAgentRuntime>(provider => new AgentRuntime(provider.GetRequiredService<IChatProviderRegistry>()));
        _ = services.AddSingleton<IConversationTitler, ConversationTitler>();
        _ = services.AddSingleton<IContextSummarizer, ContextSummarizer>();
        _ = services.AddSingleton<ConversationExportService>();
        _ = services.AddSingleton<IShareService, ShareGptShareService>();
        _ = services.AddSingleton<IImageAttachmentCache, ImageAttachmentCache>();
        _ = services.AddSingleton<IImageConverter, MagickImageConverter>();
        _ = services.AddSingleton<ImageAttachmentService>();
        _ = services.AddSingleton<SettingsRepository, ObservableSettingsRepository>();
        _ = services.AddSingleton<IAppNavigator, AppNavigator>();
        _ = services.AddSingleton<IShortcutRegistry, ShortcutRegistry>();
        _ = services.AddSingleton<IThemeService, AvaloniaThemeService>();
        _ = services.AddSingleton<ILocaleService>(_ => LocaleService.FromDirectory(CreateLocaleDirectory(), CultureInfo.CurrentUICulture.Name));
    }

    private static void AddFeatureViewModels(IServiceCollection services)
    {
        _ = services.AddTransient(provider => new SettingsViewModel(
            provider.GetRequiredService<SettingsRepository>(),
            provider.GetRequiredService<ILocaleService>(),
            provider.GetRequiredService<IThemeService>()));
        _ = services.AddTransient(
            provider =>
            {
                ConversationListViewModel viewModel = new ConversationListViewModel(
                    provider.GetRequiredService<IConversationRepository>(),
                    provider.GetRequiredService<IAppNavigator>());
                viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                return viewModel;
            });
        _ = services.AddTransient<Func<string, ChatDetailViewModel>>(
            provider => conversationId =>
            {
                ChatDetailViewModel viewModel = new ChatDetailViewModel(
                    conversationId,
                    provider.GetRequiredService<IAgentRuntime>(),
                    provider.GetRequiredService<IConversationRepository>(),
                    provider.GetRequiredService<IMessageRepository>(),
                    provider.GetRequiredService<IAppNavigator>(),
                    provider.GetRequiredService<IConversationTitler>(),
                    provider.GetRequiredService<IContextSummarizer>(),
                    provider.GetRequiredService<ConversationExportService>(),
                    provider.GetRequiredService<IShareService>(),
                    provider.GetRequiredService<ImageAttachmentService>());
                viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                return viewModel;
            });
        _ = services.AddTransient(
            provider =>
            {
                SearchChatViewModel viewModel = new SearchChatViewModel(
                    provider.GetRequiredService<IConversationRepository>(),
                    provider.GetRequiredService<IAppNavigator>());
                viewModel.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                return viewModel;
            });
        _ = services.AddTransient(provider => new MainWindowViewModel(
            provider.GetRequiredService<IAppNavigator>(),
            provider.GetRequiredService<ConversationListViewModel>(),
            provider.GetRequiredService<SettingsViewModel>(),
            provider.GetRequiredService<CommandPaletteViewModel>(),
            provider.GetRequiredService<ILocaleService>(),
            provider.GetRequiredService<Func<string, ChatDetailViewModel>>(),
            provider.GetRequiredService<SearchChatViewModel>));
        _ = services.AddTransient(provider => new CommandPaletteViewModel(provider.GetRequiredService<IShortcutRegistry>()));
    }

    private static string CreateLocaleDirectory()
    {
        string localeBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        return Path.Join(localeBaseDirectory, "Resources", "Locales");
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
