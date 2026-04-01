using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;

namespace DriverGuardian.Application.Recommendations;

public sealed class RecommendationPipeline : IRecommendationPipeline
{
    public Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers,
        CancellationToken cancellationToken)
    {
        var result = installedDrivers
            .Select(driver => new RecommendationSummary(
                driver.DeviceIdentity,
                HasRecommendation: false,
                Reason: "No provider integration in stage 0.",
                RecommendedVersion: null))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<RecommendationSummary>>(result);
    }
}
