using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.OfficialSources;

public sealed class OpenOfficialSourceActionEvaluator
{
    public OpenOfficialSourceActionDecision Evaluate(OpenOfficialSourceActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blockers = new List<OpenOfficialSourceBlocker>();

        if (request.ResolutionOutcome == OfficialSourceResolutionOutcome.InsufficientEvidence)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.ResolutionNotConfirmed,
                "Open official source action is blocked because source resolution evidence is insufficient."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.InsufficientEvidence,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        if (request.SourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.SourceTrustUnverified,
                "Open official source action is blocked because source trust could not be verified."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.InsufficientEvidence,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        if (!request.SourceEvidence.IsOfficialSource)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.SourceMarkedNonOfficial,
                "Open official source action is blocked because source evidence is not official."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.NonOfficialSource,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        if (request.OfficialSourceUri is null)
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.MissingOfficialSourceUrl,
                "Open official source action is blocked because no official source URL is available."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.MissingUrl,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        if (!string.Equals(request.OfficialSourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.UrlIsNotHttps,
                "Open official source action is blocked because official source URL is not HTTPS."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.Blocked,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        if (!string.Equals(request.OfficialSourceUri.Host, request.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(new OpenOfficialSourceBlocker(
                OpenOfficialSourceBlockedReason.UrlHostMismatch,
                "Open official source action is blocked because official source URL host does not match source evidence host."));

            return new OpenOfficialSourceActionDecision(
                OpenOfficialSourceActionOutcome.Blocked,
                request.ResolutionOutcome,
                null,
                blockers);
        }

        return new OpenOfficialSourceActionDecision(
            OpenOfficialSourceActionOutcome.Allowed,
            request.ResolutionOutcome,
            new ApprovedOfficialSourceLink(
                request.ProviderCode,
                request.DriverIdentifier,
                request.OfficialSourceUri),
            Array.Empty<OpenOfficialSourceBlocker>());
    }
}
