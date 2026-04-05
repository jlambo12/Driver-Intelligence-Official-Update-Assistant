using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Recommendations;

public sealed class RecommendationEvaluator
{
    public RecommendationDecision Evaluate(RecommendationEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Candidates.Count == 0)
        {
            return new RecommendationDecision(
                RecommendationOutcome.InsufficientEvidence,
                input.InstalledDriver.DriverVersion,
                null,
                null,
                CompatibilityConfidence.Unknown,
                null,
                [new RecommendationReason(RecommendationReasonCode.NoCandidates, "No provider candidates were returned.", null, null)]);
        }

        var reasons = new List<RecommendationReason>();
        var validCandidates = new List<RecommendationCandidateInput>();

        foreach (var candidateInput in input.Candidates)
        {
            var candidate = candidateInput.Candidate;
            if (string.IsNullOrWhiteSpace(candidate.CandidateVersion))
            {
                reasons.Add(new RecommendationReason(
                    RecommendationReasonCode.MissingCandidateVersion,
                    "Candidate was ignored because it did not include a version.",
                    candidateInput.ProviderCode,
                    null));
                continue;
            }

            validCandidates.Add(candidateInput);
        }

        if (validCandidates.Count == 0)
        {
            return new RecommendationDecision(
                RecommendationOutcome.InsufficientEvidence,
                input.InstalledDriver.DriverVersion,
                null,
                null,
                CompatibilityConfidence.Unknown,
                null,
                reasons);
        }

        var installedVersion = ParseVersion(input.InstalledDriver.DriverVersion);
        var ordered = validCandidates
            .OrderByDescending(c => GetProviderPriority(c.Candidate.SourceEvidence, input.ProviderPrecedence))
            .ThenByDescending(c => c.Candidate.CompatibilityConfidence)
            .ThenByDescending(c => ParseVersion(c.Candidate.CandidateVersion!))
            .ToArray();

        var best = ordered[0];
        var bestVersion = ParseVersion(best.Candidate.CandidateVersion!);
        var versionComparison = bestVersion.CompareTo(installedVersion);

        if (versionComparison <= 0)
        {
            reasons.Add(new RecommendationReason(
                RecommendationReasonCode.CandidateNotNewer,
                "Top candidate is not newer than the installed version.",
                best.ProviderCode,
                best.Candidate.CandidateVersion));

            return new RecommendationDecision(
                RecommendationOutcome.AlreadyUpToDate,
                input.InstalledDriver.DriverVersion,
                null,
                best.ProviderCode,
                best.Candidate.CompatibilityConfidence,
                best.Candidate.SourceEvidence,
                reasons);
        }

        if (best.Candidate.HardwareMatchQuality == HardwareMatchQuality.VendorFamilyFallback ||
            best.Candidate.HardwareMatchQuality == HardwareMatchQuality.Unknown)
        {
            reasons.Add(new RecommendationReason(
                RecommendationReasonCode.CandidateWeakHardwareMatch,
                "Candidate is newer but only a vendor-family fallback match is available.",
                best.ProviderCode,
                best.Candidate.CandidateVersion));

            return new RecommendationDecision(
                RecommendationOutcome.NotRecommended,
                input.InstalledDriver.DriverVersion,
                null,
                best.ProviderCode,
                best.Candidate.CompatibilityConfidence,
                best.Candidate.SourceEvidence,
                reasons);
        }

        if (best.Candidate.CompatibilityConfidence is CompatibilityConfidence.Unknown or CompatibilityConfidence.Low or CompatibilityConfidence.Medium)
        {
            reasons.Add(new RecommendationReason(
                RecommendationReasonCode.CandidateHasLowCompatibilityConfidence,
                "Candidate is newer but compatibility confidence is not high.",
                best.ProviderCode,
                best.Candidate.CandidateVersion));

            return new RecommendationDecision(
                RecommendationOutcome.NotRecommended,
                input.InstalledDriver.DriverVersion,
                null,
                best.ProviderCode,
                best.Candidate.CompatibilityConfidence,
                best.Candidate.SourceEvidence,
                reasons);
        }

        reasons.Add(new RecommendationReason(
            RecommendationReasonCode.CompatibleUpgradeAvailable,
            "A newer compatible candidate is available.",
            best.ProviderCode,
            best.Candidate.CandidateVersion));

        reasons.Add(new RecommendationReason(
            best.Candidate.SourceEvidence.IsOfficialSource
                ? RecommendationReasonCode.CandidateIsOfficialSource
                : RecommendationReasonCode.CandidateIsOemSource,
            best.Candidate.SourceEvidence.IsOfficialSource
                ? "Recommendation is supported by an official source."
                : "Recommendation is supported by an OEM source.",
            best.ProviderCode,
            best.Candidate.CandidateVersion));

        return new RecommendationDecision(
            RecommendationOutcome.Recommended,
            input.InstalledDriver.DriverVersion,
            best.Candidate.CandidateVersion,
            best.ProviderCode,
            best.Candidate.CompatibilityConfidence,
            best.Candidate.SourceEvidence,
            reasons);
    }

    private static Version ParseVersion(string version)
    {
        if (Version.TryParse(version, out var parsed))
        {
            return parsed;
        }

        var normalized = string.Join('.', version
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var numeric) ? numeric.ToString() : "0"));

        return Version.TryParse(normalized, out var fallback)
            ? fallback
            : new Version(0, 0);
    }

    private static int GetProviderPriority(SourceEvidence sourceEvidence, ProviderPrecedence precedence)
    {
        var basePriority = sourceEvidence.TrustLevel switch
        {
            SourceTrustLevel.OfficialPublisherSite => 30,
            SourceTrustLevel.OemSupportPortal => 20,
            SourceTrustLevel.OperatingSystemCatalog => 10,
            _ => 0
        };

        return precedence switch
        {
            ProviderPrecedence.OfficialFirst when sourceEvidence.IsOfficialSource => basePriority + 10,
            ProviderPrecedence.OemFirst when sourceEvidence.TrustLevel == SourceTrustLevel.OemSupportPortal => basePriority + 10,
            ProviderPrecedence.Neutral => basePriority,
            _ => basePriority
        };
    }
}
