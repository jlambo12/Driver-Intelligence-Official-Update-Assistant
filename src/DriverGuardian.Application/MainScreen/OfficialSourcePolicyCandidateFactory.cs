using DriverGuardian.Application.Abstractions;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.MainScreen;

internal static class OfficialSourcePolicyCandidateFactory
{
    // Safety-first policy:
    // 1) source evidence page is the trust anchor.
    // 2) approved navigation target is never replaced by an external CDN download URI.
    // 3) direct official driver page is approved only for strong official publisher trust.
    // 4) vendor support / catalog trust resolves to source page navigation.
    public static bool TryBuild(
        string providerCode,
        ProviderCandidate candidate,
        out OfficialSourcePolicyCandidate policyCandidate)
    {
        var sourceEvidence = candidate.SourceEvidence;
        if (!sourceEvidence.IsOfficialSource || sourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            policyCandidate = null!;
            return false;
        }

        if (!string.Equals(sourceEvidence.SourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            policyCandidate = null!;
            return false;
        }

        var actionTarget = sourceEvidence.TrustLevel == SourceTrustLevel.OfficialPublisherSite
            ? OfficialSourceActionTarget.DirectDownloadPage
            : OfficialSourceActionTarget.SourcePage;

        policyCandidate = new OfficialSourcePolicyCandidate(
            providerCode,
            candidate.DriverIdentifier,
            sourceEvidence,
            SourceEvidencePageUri: sourceEvidence.SourceUri,
            ApprovedNavigationUri: sourceEvidence.SourceUri,
            RawDownloadUri: candidate.DownloadUri,
            actionTarget,
            OfficialSourcePolicyScorer.Calculate(sourceEvidence.TrustLevel, candidate.CompatibilityConfidence));

        return true;
    }
}
