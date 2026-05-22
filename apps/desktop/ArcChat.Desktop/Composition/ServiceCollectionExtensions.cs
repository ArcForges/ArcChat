// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Globalization;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Desktop.Composition;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddArcChatDesktop(this IServiceCollection services)
    {
        _ = services.AddSingleton<IAppNavigator, AppNavigator>();
        _ = services.AddSingleton<ILocaleService>(
            _ => LocaleService.FromDirectory(
                Path.Combine(AppContext.BaseDirectory, "Resources", "Locales"),
                CultureInfo.CurrentUICulture.Name));
        _ = services.AddTransient<MainWindowViewModel>();
        return services;
    }
}
