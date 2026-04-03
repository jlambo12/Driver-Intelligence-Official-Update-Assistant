using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.OfficialSources;

public sealed class OpenOfficialSourceActionEvaluator
{
    public OpenOfficialSourceActionDecision Evaluate(OpenOfficialSourceActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blockers = new List<OpenOfficialSourceBlocker>();

        if (request.SourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.SourceTrustUnverified,
                "Open official source action is blocked because source trust could not be verified."));

            return BuildInsufficientEvidenceDecision(blockers);
        }

        if (!request.SourceEvidence.IsOfficialSource)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.SourceMarkedNonOfficial,
                "Open official source action is blocked because source evidence is not official."));

            return BuildInsufficientEvidenceDecision(blockers, OpenOfficialSourceActionOutcome.NonOfficialSource);
        }

        if (request.OfficialSourceUri is null)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.MissingOfficialSourceUrl,
                "Open official source action is blocked because no official source URL is available."));

            return BuildInsufficientEvidenceDecision(blockers, OpenOfficialSourceActionOutcome.MissingUrl);
        }

        if (!string.Equals(request.OfficialSourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.UrlIsNotHttps,
                "Open official source action is blocked because official source URL is not HTTPS."));

            return BuildInsufficientEvidenceDecision(blockers, OpenOfficialSourceActionOutcome.Blocked);
        }

        if (!request.AllowDifferentHostOfficialDownload
            && !string.Equals(request.OfficialSourceUri.Host, request.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.UrlHostMismatch,
                "Open official source action is blocked because official source URL host does not match source evidence host."));

            return BuildInsufficientEvidenceDecision(blockers, OpenOfficialSourceActionOutcome.Blocked);
        }

        if (!TryResolveConfirmedOutcome(request.SourceEvidence.TrustLevel, out var resolutionOutcome))
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.UnsupportedSourceTrustLevel,
                "Open official source action is blocked because source trust level does not classify the page as a confirmed direct driver page or vendor support page."));

            return BuildInsufficientEvidenceDecision(blockers);
        }

        return new OpenOfficialSourceActionDecision(
            OpenOfficialSourceActionOutcome.Allowed,
            resolutionOutcome,
            new ApprovedOfficialSourceLink(
                request.ProviderCode,
                request.DriverIdentifier,
                request.OfficialSourceUri),
            Array.Empty<OpenOfficialSourceBlocker>());
    }

    private static bool TryResolveConfirmedOutcome(SourceTrustLevel trustLevel, out OfficialSourceResolutionOutcome outcome)
    {
        switch (trustLevel)
        {
            case SourceTrustLevel.OfficialPublisherSite:
                outcome = OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage;
                return true;
            case SourceTrustLevel.OemSupportPortal:
            case SourceTrustLevel.OperatingSystemCatalog:
                outcome = OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage;
                return true;
            default:
                outcome = OfficialSourceResolutionOutcome.InsufficientEvidence;
                return false;
        }
    }

    private static OpenOfficialSourceActionDecision BuildInsufficientEvidenceDecision(
        IReadOnlyCollection<OpenOfficialSourceBlocker> blockers,
        OpenOfficialSourceActionOutcome outcome = OpenOfficialSourceActionOutcome.InsufficientEvidence)
        => new(
            outcome,
            OfficialSourceResolutionOutcome.InsufficientEvidence,
            null,
            blockers);
}
