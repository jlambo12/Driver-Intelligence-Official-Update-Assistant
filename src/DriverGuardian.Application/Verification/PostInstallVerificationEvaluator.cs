using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.Application.Verification;

public sealed class PostInstallVerificationEvaluator
{
    public PostInstallVerificationResult Evaluate(PostInstallVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Baseline is null)
        {
            return new PostInstallVerificationResult(
                PostInstallVerificationOutcome.InsufficientEvidence,
                PostInstallVerificationReason.MissingBaselineSnapshot,
                null,
                "Unable to verify manual installation because no baseline snapshot is available.");
        }

        if (request.CurrentSnapshot is null)
        {
            return new PostInstallVerificationResult(
                PostInstallVerificationOutcome.DeviceMissing,
                PostInstallVerificationReason.DeviceNotPresentAfterManualInstall,
                null,
                "Unable to locate the device after manual installation. The device may be disconnected or unavailable.");
        }

        if (!Equals(request.Baseline.Snapshot.DeviceIdentity, request.DeviceIdentity) ||
            !Equals(request.CurrentSnapshot.DeviceIdentity, request.DeviceIdentity))
        {
            return new PostInstallVerificationResult(
                PostInstallVerificationOutcome.InsufficientEvidence,
                PostInstallVerificationReason.DeviceIdentityMismatch,
                null,
                "Unable to verify manual installation because the compared snapshots do not match the requested device identity.");
        }

        if (string.IsNullOrWhiteSpace(request.Baseline.Snapshot.DriverVersion) ||
            string.IsNullOrWhiteSpace(request.CurrentSnapshot.DriverVersion))
        {
            return new PostInstallVerificationResult(
                PostInstallVerificationOutcome.InsufficientEvidence,
                PostInstallVerificationReason.MissingDriverVersionEvidence,
                null,
                "Unable to verify manual installation because driver version evidence is incomplete.");
        }

        var comparison = Compare(request.Baseline.Snapshot, request.CurrentSnapshot);

        if (!comparison.HasAnyChange)
        {
            return new PostInstallVerificationResult(
                PostInstallVerificationOutcome.NoChangeDetected,
                PostInstallVerificationReason.None,
                comparison,
                "No detectable driver differences were found after manual installation.");
        }

        if (comparison.VersionChanged)
        {
            return new PostInstallVerificationResult(
                comparison.ProviderChanged || comparison.DateChanged
                    ? PostInstallVerificationOutcome.VerifiedChanged
                    : PostInstallVerificationOutcome.PartiallyChanged,
                PostInstallVerificationReason.None,
                comparison,
                comparison.ProviderChanged || comparison.DateChanged
                    ? "Manual installation appears successful. Version and additional driver attributes changed."
                    : "Manual installation changed the driver version, but not all attributes changed.");
        }

        return new PostInstallVerificationResult(
            PostInstallVerificationOutcome.PartiallyChanged,
            PostInstallVerificationReason.None,
            comparison,
            "Some driver attributes changed, but version did not change. Verification is partial.");
    }

    private static DriverStateComparisonResult Compare(InstalledDriverSnapshot baseline, InstalledDriverSnapshot current)
    {
        var differences = new List<DriverDifferenceDetail>();

        var versionChanged = !string.Equals(baseline.DriverVersion, current.DriverVersion, StringComparison.OrdinalIgnoreCase);
        if (versionChanged)
        {
            differences.Add(new DriverDifferenceDetail(
                DriverDifferenceKind.VersionChanged,
                baseline.DriverVersion,
                current.DriverVersion,
                "Driver version changed."));
        }

        var providerChanged = !string.Equals(Normalize(baseline.ProviderName), Normalize(current.ProviderName), StringComparison.OrdinalIgnoreCase);
        if (providerChanged)
        {
            differences.Add(new DriverDifferenceDetail(
                DriverDifferenceKind.ProviderChanged,
                baseline.ProviderName,
                current.ProviderName,
                "Driver provider changed."));
        }

        var dateChanged = baseline.DriverDate != current.DriverDate;
        if (dateChanged)
        {
            differences.Add(new DriverDifferenceDetail(
                DriverDifferenceKind.DateChanged,
                baseline.DriverDate?.ToString("yyyy-MM-dd"),
                current.DriverDate?.ToString("yyyy-MM-dd"),
                "Driver date changed."));
        }

        if (differences.Count == 0)
        {
            differences.Add(new DriverDifferenceDetail(
                DriverDifferenceKind.NoDetectableDifference,
                null,
                null,
                "No detectable difference."));
        }

        return new DriverStateComparisonResult(differences, versionChanged, providerChanged, dateChanged);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
