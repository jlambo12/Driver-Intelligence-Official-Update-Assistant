using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Tests.Unit.Application.Verification;

public sealed class PostInstallVerificationEvaluatorTests
{
    private static readonly DeviceIdentity TestDeviceIdentity = new("PCI\\VEN_8086&DEV_15F3");
    private static readonly HardwareIdentifier TestHardwareIdentifier = new("PCI\\VEN_8086&DEV_15F3");

    private readonly PostInstallVerificationEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsVerifiedChanged_WhenVersionAndAdditionalAttributesChange()
    {
        var result = _evaluator.Evaluate(new PostInstallVerificationRequest(
            TestDeviceIdentity,
            new VerificationBaselineSnapshot(CreateSnapshot("1.0.0", new DateOnly(2024, 1, 1), "Provider A"), DateTimeOffset.UtcNow.AddMinutes(-20)),
            CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider B")));

        Assert.Equal(PostInstallVerificationOutcome.VerifiedChanged, result.Outcome);
        Assert.True(result.IsVerifiedChanged);
        Assert.NotNull(result.Comparison);
        Assert.Contains(result.Comparison!.Differences, x => x.Kind == DriverDifferenceKind.VersionChanged);
        Assert.Contains(result.Comparison.Differences, x => x.Kind == DriverDifferenceKind.ProviderChanged);
        Assert.Contains(result.Comparison.Differences, x => x.Kind == DriverDifferenceKind.DateChanged);
    }

    [Fact]
    public void Evaluate_ReturnsNoChangeDetected_WhenNoDifferencesAreFound()
    {
        var snapshot = CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider A");

        var result = _evaluator.Evaluate(new PostInstallVerificationRequest(
            TestDeviceIdentity,
            new VerificationBaselineSnapshot(snapshot, DateTimeOffset.UtcNow.AddMinutes(-20)),
            snapshot));

        Assert.Equal(PostInstallVerificationOutcome.NoChangeDetected, result.Outcome);
        Assert.NotNull(result.Comparison);
        Assert.Contains(result.Comparison!.Differences, x => x.Kind == DriverDifferenceKind.NoDetectableDifference);
    }

    [Fact]
    public void Evaluate_ReturnsPartiallyChanged_WhenOnlyProviderChanges()
    {
        var result = _evaluator.Evaluate(new PostInstallVerificationRequest(
            TestDeviceIdentity,
            new VerificationBaselineSnapshot(CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider A"), DateTimeOffset.UtcNow.AddMinutes(-20)),
            CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider B")));

        Assert.Equal(PostInstallVerificationOutcome.PartiallyChanged, result.Outcome);
        Assert.NotNull(result.Comparison);
        Assert.True(result.Comparison!.ProviderChanged);
        Assert.False(result.Comparison.VersionChanged);
    }

    [Fact]
    public void Evaluate_ReturnsDeviceMissing_WhenPostInstallSnapshotIsMissing()
    {
        var result = _evaluator.Evaluate(new PostInstallVerificationRequest(
            TestDeviceIdentity,
            new VerificationBaselineSnapshot(CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider A"), DateTimeOffset.UtcNow.AddMinutes(-20)),
            null));

        Assert.Equal(PostInstallVerificationOutcome.DeviceMissing, result.Outcome);
        Assert.Equal(PostInstallVerificationReason.DeviceNotPresentAfterManualInstall, result.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsInsufficientEvidence_WhenBaselineIsMissing()
    {
        var result = _evaluator.Evaluate(new PostInstallVerificationRequest(TestDeviceIdentity, null, CreateSnapshot("2.0.0", new DateOnly(2025, 3, 12), "Provider A")));

        Assert.Equal(PostInstallVerificationOutcome.InsufficientEvidence, result.Outcome);
        Assert.Equal(PostInstallVerificationReason.MissingBaselineSnapshot, result.Reason);
    }

    private static InstalledDriverSnapshot CreateSnapshot(string version, DateOnly? date, string provider)
        => new(TestDeviceIdentity, TestHardwareIdentifier, version, date, provider);
}
