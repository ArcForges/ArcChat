// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ArcChat.Desktop.Composition;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddArcChatDesktop(this IServiceCollection services)
    {
        _ = services.AddSingleton<IAppNavigator, AppNavigator>();
        _ = services.AddTransient<MainWindowViewModel>();
        return services;
    }
}
