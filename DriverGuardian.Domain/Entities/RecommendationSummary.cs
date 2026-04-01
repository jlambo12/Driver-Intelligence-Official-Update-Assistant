using DriverGuardian.Domain.Enums;

namespace DriverGuardian.Domain.Entities;

public sealed record RecommendationSummary(
    Guid SessionId,
    int TotalDevices,
    int PotentiallyOutdatedCount,
    CompatibilityConfidenceLevel OverallConfidence,
    bool RequiresManualVerification,
    string MachineReadableReasonCode);
