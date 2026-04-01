using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.ProviderAdapters.Abstractions.Models;

public sealed record CompatibilityAssessmentResult(
    CompatibilityConfidence Confidence,
    bool RequiresManualVerification,
    string ReasonCode);
