using DriverGuardian.Domain.Common;
using DriverGuardian.Domain.Enums;

namespace DriverGuardian.Domain.ValueObjects;

public sealed record CompatibilityConfidence
{
    public CompatibilityConfidenceLevel Level { get; }
    public decimal Score { get; }

    public CompatibilityConfidence(CompatibilityConfidenceLevel level, decimal score)
    {
        if (score is < 0 or > 1)
            throw new DomainException("Confidence score must be between 0 and 1.");

        Level = level;
        Score = score;
    }
}
