using DriverGuardian.Application.Abstractions;
using DriverGuardian.Infrastructure.Abstractions;
using DriverGuardian.Infrastructure.Logging;
using DriverGuardian.Infrastructure.Persistence;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DriverGuardian.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureFoundation(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>();
        services.AddSingleton<IAuditPersistence, InMemoryAuditPersistence>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        return services;
    }
}
