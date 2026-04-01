using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DriverGuardian.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddScoped<IDriverInspectionOrchestrator, DriverInspectionOrchestrator>();
        services.AddScoped<IScanOrchestrator, ScanOrchestrator>();
        services.AddScoped<IPostScanSummaryBuilder, PostScanSummaryBuilder>();
        services.AddScoped<IRecommendationPipeline, RecommendationPipeline>();
        return services;
    }
}
