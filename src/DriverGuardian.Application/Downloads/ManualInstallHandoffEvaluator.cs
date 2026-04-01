namespace DriverGuardian.Application.Downloads;

public sealed class ManualInstallHandoffEvaluator
{
    public ManualInstallHandoffDecision Evaluate(ManualInstallHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reasons = new List<UserActionRequiredReason>();
        var candidate = request.Candidate;

        if (candidate.SourceEvidence.TrustLevel == ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.Unknown)
        {
            reasons.Add(new UserActionRequiredReason(
                HandoffBlockReason.SourceTrustUnverified,
                "Manual install handoff requires user action because source trust could not be verified."));

            return new ManualInstallHandoffDecision(HandoffReadinessOutcome.InsufficientEvidence, null, reasons);
        }

        if (!candidate.SourceEvidence.IsOfficialSource)
        {
            reasons.Add(new UserActionRequiredReason(
                HandoffBlockReason.SourceMarkedNonOfficial,
                "Manual install handoff requires user action because source evidence is not official."));

            return new ManualInstallHandoffDecision(HandoffReadinessOutcome.NonOfficialSource, null, reasons);
        }

        if (candidate.DownloadUri is null)
        {
            reasons.Add(new UserActionRequiredReason(
                HandoffBlockReason.MissingOfficialPackageUrl,
                "Manual install handoff requires user action because no official package reference URL is available."));

            return new ManualInstallHandoffDecision(HandoffReadinessOutcome.MissingOfficialPackageReference, null, reasons);
        }

        if (!string.Equals(candidate.DownloadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(new UserActionRequiredReason(
                HandoffBlockReason.PackageUrlIsNotHttps,
                "Manual install handoff requires user action because the official package reference URL is not HTTPS."));

            return new ManualInstallHandoffDecision(HandoffReadinessOutcome.UserActionRequired, null, reasons);
        }

        if (!string.Equals(candidate.DownloadUri.Host, candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(new UserActionRequiredReason(
                HandoffBlockReason.PackageUrlHostMismatch,
                "Manual install handoff requires user action because package URL host does not match source evidence host."));

            return new ManualInstallHandoffDecision(HandoffReadinessOutcome.UserActionRequired, null, reasons);
        }

        return new ManualInstallHandoffDecision(
            HandoffReadinessOutcome.ReadyForManualInstallHandoff,
            new OfficialPackageReference(
                request.ProviderCode,
                candidate.DriverIdentifier,
                candidate.CandidateVersion,
                candidate.DownloadUri,
                candidate.SourceEvidence),
            Array.Empty<UserActionRequiredReason>());
    }
}
