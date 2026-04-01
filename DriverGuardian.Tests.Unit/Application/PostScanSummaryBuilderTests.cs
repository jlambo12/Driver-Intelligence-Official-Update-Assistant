using DriverGuardian.Application.Services;
using DriverGuardian.Domain.Entities;
using DriverGuardian.Domain.Enums;
using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class PostScanSummaryBuilderTests
{
    [Fact]
    public void Build_Should_MarkManualVerificationRequired()
    {
        var snapshot = new InstalledDriverSnapshot(
            new DeviceIdentity("X", "Y"),
            [new HardwareIdentifier("PCI\\VEN_1")],
            "1.0",
            null,
            "Provider",
            DriverSourceProvenance.Unknown,
            new CompatibilityConfidence(CompatibilityConfidenceLevel.Low, 0.2m),
            false,
            null);

        var session = new ScanSession(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, [snapshot], false);

        var sut = new PostScanSummaryBuilder();
        var result = sut.Build(session);

        Assert.True(result.RequiresManualVerification);
        Assert.Equal("ANALYSIS_ONLY_MODE", result.MachineReadableReasonCode);
    }
}
