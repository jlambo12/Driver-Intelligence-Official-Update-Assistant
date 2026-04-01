using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Abstractions;

public interface IPostScanSummaryBuilder
{
    RecommendationSummary Build(ScanSession session);
}
