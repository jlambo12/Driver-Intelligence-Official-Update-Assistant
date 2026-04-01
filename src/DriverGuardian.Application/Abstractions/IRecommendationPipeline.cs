using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;

namespace DriverGuardian.Application.Abstractions;

public interface IRecommendationPipeline
{
    Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers,
        CancellationToken cancellationToken);
}
