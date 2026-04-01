using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Verification;

public enum PostInstallVerificationOutcome
{
    VerifiedChanged = 0,
    NoChangeDetected = 1,
    PartiallyChanged = 2,
    InsufficientEvidence = 3,
    DeviceMissing = 4
}

public enum PostInstallVerificationReason
{
    None = 0,
    MissingBaselineSnapshot = 1,
    MissingPostInstallSnapshot = 2,
    DeviceNotPresentAfterManualInstall = 3,
    MissingDriverVersionEvidence = 4,
    DeviceIdentityMismatch = 5
}

public enum DriverDifferenceKind
{
    VersionChanged = 0,
    ProviderChanged = 1,
    DateChanged = 2,
    NoDetectableDifference = 3
}

public sealed record DriverDifferenceDetail(
    DriverDifferenceKind Kind,
    string? BeforeValue,
    string? AfterValue,
    string Description);

public sealed record PostInstallVerificationRequest(
    DeviceIdentity DeviceIdentity,
    VerificationBaselineSnapshot? Baseline,
    InstalledDriverSnapshot? CurrentSnapshot);

public sealed record VerificationBaselineSnapshot(
    InstalledDriverSnapshot Snapshot,
    DateTimeOffset CapturedAtUtc);

public sealed record DriverStateComparisonResult(
    IReadOnlyCollection<DriverDifferenceDetail> Differences,
    bool VersionChanged,
    bool ProviderChanged,
    bool DateChanged)
{
    public bool HasAnyChange => VersionChanged || ProviderChanged || DateChanged;
}

public sealed record PostInstallVerificationResult(
    PostInstallVerificationOutcome Outcome,
    PostInstallVerificationReason Reason,
    DriverStateComparisonResult? Comparison,
    string Message)
{
    public bool IsVerifiedChanged => Outcome == PostInstallVerificationOutcome.VerifiedChanged;
}
