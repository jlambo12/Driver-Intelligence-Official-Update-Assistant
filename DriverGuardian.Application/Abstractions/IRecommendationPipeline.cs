using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Abstractions;

public interface IRecommendationPipeline
{
    Task<RecommendationSummary> BuildAsync(ScanSession session, CancellationToken cancellationToken);
}
