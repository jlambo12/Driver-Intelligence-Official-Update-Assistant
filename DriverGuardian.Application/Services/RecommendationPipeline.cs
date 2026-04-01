using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Services;

public sealed class RecommendationPipeline(IPostScanSummaryBuilder summaryBuilder) : IRecommendationPipeline
{
    public Task<RecommendationSummary> BuildAsync(ScanSession session, CancellationToken cancellationToken)
        => Task.FromResult(summaryBuilder.Build(session));
}
