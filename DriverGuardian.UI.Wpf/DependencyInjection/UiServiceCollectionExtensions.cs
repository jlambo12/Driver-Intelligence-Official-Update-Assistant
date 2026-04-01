using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DriverGuardian.UI.Wpf.DependencyInjection;

public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddUiFoundation(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizedTextProvider, ResxLocalizedTextProvider>();
        services.AddSingleton<LocalizedStrings>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services;
    }
}
