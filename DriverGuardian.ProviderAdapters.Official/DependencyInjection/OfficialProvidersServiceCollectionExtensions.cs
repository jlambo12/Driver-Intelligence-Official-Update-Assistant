using DriverGuardian.Application.Abstractions;
using DriverGuardian.ProviderAdapters.Abstractions.Contracts;
using DriverGuardian.ProviderAdapters.Official.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DriverGuardian.ProviderAdapters.Official.DependencyInjection;

public static class OfficialProvidersServiceCollectionExtensions
{
    public static IServiceCollection AddOfficialProviderAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IOemPriorityStrategy, StaticOemPriorityStrategy>();
        services.AddSingleton<IOfficialDriverProviderAdapter, OfficialProviderStubAdapter>();
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        return services;
    }
}
