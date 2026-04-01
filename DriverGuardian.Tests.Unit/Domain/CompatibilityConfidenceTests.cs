using DriverGuardian.Domain.Common;
using DriverGuardian.Domain.Enums;
using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.Tests.Unit.Domain;

public sealed class CompatibilityConfidenceTests
{
    [Fact]
    public void Constructor_Should_Throw_WhenScoreOutOfRange()
    {
        Assert.Throws<DomainException>(() => new CompatibilityConfidence(CompatibilityConfidenceLevel.High, 1.2m));
    }
}
