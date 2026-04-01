namespace DriverGuardian.Application.Downloads;

public sealed class SafeDownloadPreparationEvaluator
{
    public DownloadDecision Evaluate(DownloadPreparationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reasons = new List<DownloadDecisionReason>();
        var candidate = request.Candidate;

        if (candidate.SourceEvidence.TrustLevel == ProviderAdapters.Abstractions.Lookup.SourceTrustLevel.Unknown)
        {
            reasons.Add(new DownloadDecisionReason(
                BlockedDownloadReason.SourceTrustUnverified,
                "Candidate is blocked because source trust could not be verified."));

            return new DownloadDecision(DownloadDecisionOutcome.InsufficientEvidence, null, reasons);
        }

        if (!candidate.SourceEvidence.IsOfficialSource)
        {
            reasons.Add(new DownloadDecisionReason(
                BlockedDownloadReason.SourceMarkedNonOfficial,
                "Candidate is blocked because source evidence is not official."));

            return new DownloadDecision(DownloadDecisionOutcome.NonOfficialSource, null, reasons);
        }

        if (candidate.DownloadUri is null)
        {
            reasons.Add(new DownloadDecisionReason(
                BlockedDownloadReason.MissingDownloadUrl,
                "Candidate is blocked because it does not include a download URL."));

            return new DownloadDecision(DownloadDecisionOutcome.MissingUrl, null, reasons);
        }

        if (!string.Equals(candidate.DownloadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(new DownloadDecisionReason(
                BlockedDownloadReason.DownloadUrlIsNotHttps,
                "Candidate is blocked because the download URL is not HTTPS."));

            return new DownloadDecision(DownloadDecisionOutcome.Blocked, null, reasons);
        }

        if (!string.Equals(candidate.DownloadUri.Host, candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(new DownloadDecisionReason(
                BlockedDownloadReason.DownloadUrlHostMismatch,
                "Candidate is blocked because download URL host does not match source evidence host."));

            return new DownloadDecision(DownloadDecisionOutcome.Blocked, null, reasons);
        }

        return new DownloadDecision(
            DownloadDecisionOutcome.Allowed,
            new DownloadCandidate(
                request.ProviderCode,
                candidate.DriverIdentifier,
                candidate.CandidateVersion,
                candidate.DownloadUri,
                candidate.SourceEvidence),
            Array.Empty<DownloadDecisionReason>());
    }
}
