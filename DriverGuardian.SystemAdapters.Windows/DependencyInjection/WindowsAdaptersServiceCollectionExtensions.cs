using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.SystemAdapters.Windows.Mappers;
using DriverGuardian.SystemAdapters.Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DriverGuardian.SystemAdapters.Windows.DependencyInjection;

public static class WindowsAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddWindowsSystemAdapters(this IServiceCollection services)
    {
        services.AddScoped<IDeviceDiscovery, StubWindowsDeviceDiscovery>();
        services.AddScoped<IDriverMetadataInspector, StubWindowsDriverMetadataInspector>();
        services.AddScoped<IDriverNormalizationMapper, DefaultDriverNormalizationMapper>();
        return services;
    }
}
