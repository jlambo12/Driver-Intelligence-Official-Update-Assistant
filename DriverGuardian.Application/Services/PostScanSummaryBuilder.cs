using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Entities;
using DriverGuardian.Domain.Enums;

namespace DriverGuardian.Application.Services;

public sealed class PostScanSummaryBuilder : IPostScanSummaryBuilder
{
    public RecommendationSummary Build(ScanSession session)
    {
        var total = session.Snapshots.Count;
        var outdated = session.Snapshots.Count(x => x.CompatibilityConfidence.Level is CompatibilityConfidenceLevel.Low or CompatibilityConfidenceLevel.Ambiguous);

        return new RecommendationSummary(
            SessionId: session.SessionId,
            TotalDevices: total,
            PotentiallyOutdatedCount: outdated,
            OverallConfidence: outdated == 0 ? CompatibilityConfidenceLevel.Medium : CompatibilityConfidenceLevel.Ambiguous,
            RequiresManualVerification: true,
            MachineReadableReasonCode: "ANALYSIS_ONLY_MODE");
    }
}
