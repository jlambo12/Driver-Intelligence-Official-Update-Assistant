using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Infrastructure.Abstractions;
using DriverGuardian.Infrastructure.Logging;
using DriverGuardian.Infrastructure.Logging.Context;
using DriverGuardian.Infrastructure.Logging.ErrorHandling;
using DriverGuardian.Infrastructure.Logging.Sanitization;
using DriverGuardian.Infrastructure.Logging.Sinks;
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
        services.AddSingleton<IAppClock, AppClockAdapter>();
        services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>();
        services.AddSingleton<IAuditPersistence, InMemoryAuditPersistence>();

        services.AddSingleton(MetadataSanitizationPolicy.Default);
        services.AddSingleton<IMetadataSanitizer, DefaultMetadataSanitizer>();
        services.AddSingleton<IOperationContextAccessor, AsyncLocalOperationContextAccessor>();
        services.AddSingleton<IOperationContextFactory, OperationContextFactory>();
        services.AddSingleton<IErrorNormalizer, DefaultErrorNormalizer>();

        services.AddSingleton<InMemoryLogSink>();
        services.AddSingleton<ILogSink>(provider => provider.GetRequiredService<InMemoryLogSink>());
        services.AddSingleton<ILogDiagnosticsQuery>(provider => provider.GetRequiredService<InMemoryLogSink>());

        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(provider => provider.GetRequiredService<InMemoryAuditSink>());

        services.AddScoped<IAppLogger, CentralizedAppLogger>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
