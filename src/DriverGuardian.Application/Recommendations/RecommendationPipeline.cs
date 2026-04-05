using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Application.Recommendations;

public sealed class RecommendationPipeline : IRecommendationPipeline
{
    private readonly RecommendationEvaluator _evaluator;
    private readonly ProviderLookupOrchestrator _lookupOrchestrator;
    private readonly ProviderPrecedence _providerPrecedence;

    public RecommendationPipeline(
        IEnumerable<IOfficialProviderAdapter>? providers = null,
        RecommendationEvaluator? evaluator = null,
        ProviderPrecedence providerPrecedence = ProviderPrecedence.OfficialFirst)
    {
        _lookupOrchestrator = new ProviderLookupOrchestrator(providers ?? []);
        _evaluator = evaluator ?? new RecommendationEvaluator();
        _providerPrecedence = providerPrecedence;
    }

    public async Task<IReadOnlyCollection<RecommendationSummary>> BuildAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> installedDrivers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(installedDrivers);

        if (installedDrivers.Count == 0)
        {
            return [];
        }

        var recommendations = new List<RecommendationSummary>(installedDrivers.Count);

        foreach (var installedDriver in installedDrivers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ShouldRunLookup(installedDriver))
            {
                recommendations.Add(MapSkippedSummary(installedDriver));
                continue;
            }

            var lookup = await _lookupOrchestrator.LookupAsync(installedDriver, cancellationToken);
            var decision = _evaluator.Evaluate(new RecommendationEvaluationInput(
                installedDriver,
                lookup.Candidates,
                _providerPrecedence));

            recommendations.Add(MapSummary(installedDriver, decision, lookup.Failures));
        }

        return recommendations;
    }


    private static bool ShouldRunLookup(InstalledDriverSnapshot installedDriver)
    {
        var inferredClass = InferDeviceClass(installedDriver);
        var friendlySignal = BuildFriendlySignal(installedDriver);

        var classification = DeviceRelevanceClassifier.Classify(
            deviceClass: inferredClass,
            instanceId: installedDriver.DeviceIdentity.InstanceId,
            hardwareIds: [installedDriver.HardwareIdentifier.Value],
            manufacturer: installedDriver.ProviderName,
            friendlyName: friendlySignal);

        return !classification.IsVirtualOrSoftware && !classification.IsLowValueTechnical;
    }

    private static string? InferDeviceClass(InstalledDriverSnapshot installedDriver)
    {
        var instanceId = installedDriver.DeviceIdentity.InstanceId;
        var hardwareId = installedDriver.HardwareIdentifier.Value;

        if (ContainsAny(instanceId, hardwareId, "swd\\mmdevapi", "hdaudio\\"))
        {
            return "AudioEndpoint";
        }

        if (ContainsAny(instanceId, hardwareId, "display", "graphics", "geforce", "radeon", "arc"))
        {
            return "Display";
        }

        if (ContainsAny(instanceId, hardwareId, "wireless", "wi-fi", "wlan", "802.11"))
        {
            return "Net";
        }

        if (ContainsAny(instanceId, hardwareId, "ethernet", "lan", "gbe"))
        {
            return "Net";
        }

        if (ContainsAny(instanceId, hardwareId, "nvme", "ahci", "raid", "sata", "scsi"))
        {
            return "SCSIAdapter";
        }

        if (ContainsAny(instanceId, hardwareId, "usb\\", "xhci", "ehci"))
        {
            return "USB";
        }

        if (ContainsAny(instanceId, hardwareId, "acpi\\", "smbus", "management engine", "amd psp", "serial io", "chipset"))
        {
            return "System";
        }

        return null;
    }

    private static string BuildFriendlySignal(InstalledDriverSnapshot installedDriver)
        => $"{installedDriver.ProviderName} {installedDriver.DeviceIdentity.InstanceId} {installedDriver.HardwareIdentifier.Value}";

    private static bool ContainsAny(string value1, string value2, params string[] keywords)
        => keywords.Any(keyword =>
            value1.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            value2.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static RecommendationSummary MapSkippedSummary(InstalledDriverSnapshot installedDriver)
        => new(
            installedDriver.DeviceIdentity,
            hasRecommendation: false,
            "No recommendation: low-value technical device skipped for deep provider lookup.",
            recommendedVersion: null,
            officialSourceUrl: null,
            RecommendationSummaryReasonCode.InsufficientEvidence);

    private static RecommendationSummary MapSummary(
        InstalledDriverSnapshot installedDriver,
        RecommendationDecision decision,
        IReadOnlyCollection<ProviderLookupFailure> failures)
    {
        var reason = BuildReason(decision, failures);
        var recommendedVersion = decision.IsRecommendation ? decision.RecommendedVersion : null;
        var officialSourceUrl = ResolveOfficialSourceUrl(decision);

        return new RecommendationSummary(
            installedDriver.DeviceIdentity,
            hasRecommendation: decision.IsRecommendation,
            reason,
            recommendedVersion,
            officialSourceUrl,
            MapReasonCode(decision.Outcome, failures));
    }

    private static string? ResolveOfficialSourceUrl(RecommendationDecision decision)
    {
        if (!decision.IsRecommendation || decision.SourceEvidence is null)
        {
            return null;
        }

        var sourceEvidence = decision.SourceEvidence;
        var uri = sourceEvidence.SourceUri;

        if (!sourceEvidence.IsOfficialSource || sourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            return null;
        }

        if (!uri.IsAbsoluteUri ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.IsLoopback ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return null;
        }

        return uri.AbsoluteUri;
    }

    private static string BuildReason(RecommendationDecision decision, IReadOnlyCollection<ProviderLookupFailure> failures)
    {
        return decision.Outcome switch
        {
            RecommendationOutcome.Recommended =>
                $"Recommended: newer compatible version {decision.RecommendedVersion} is available.",
            RecommendationOutcome.AlreadyUpToDate =>
                "No recommendation: installed driver is already up to date.",
            RecommendationOutcome.Incompatible =>
                "No recommendation: available candidate is marked as incompatible.",
            RecommendationOutcome.NotRecommended =>
                "No recommendation: candidate confidence is below strict recommendation threshold.",
            RecommendationOutcome.InsufficientEvidence when failures.Count > 0 =>
                $"No recommendation: insufficient evidence because provider lookup failed ({failures.First().ProviderCode}: {failures.First().FailureReason}).",
            RecommendationOutcome.InsufficientEvidence =>
                "No recommendation: insufficient evidence from providers.",
            _ =>
                "No recommendation: insufficient evidence from providers."
        };
    }

    private static RecommendationSummaryReasonCode MapReasonCode(
        RecommendationOutcome outcome,
        IReadOnlyCollection<ProviderLookupFailure> failures)
    {
        return outcome switch
        {
            RecommendationOutcome.Recommended => RecommendationSummaryReasonCode.RecommendedUpgradeAvailable,
            RecommendationOutcome.AlreadyUpToDate => RecommendationSummaryReasonCode.AlreadyUpToDate,
            RecommendationOutcome.Incompatible => RecommendationSummaryReasonCode.CandidateMarkedIncompatible,
            RecommendationOutcome.NotRecommended => RecommendationSummaryReasonCode.CandidateCompatibilityUnknown,
            RecommendationOutcome.InsufficientEvidence when failures.Count > 0 =>
                RecommendationSummaryReasonCode.InsufficientEvidenceDueToProviderFailures,
            RecommendationOutcome.InsufficientEvidence => RecommendationSummaryReasonCode.InsufficientEvidence,
            _ => RecommendationSummaryReasonCode.Unknown
        };
    }

    private sealed class ProviderLookupOrchestrator(IEnumerable<IOfficialProviderAdapter> providers)
    {
        private readonly IReadOnlyCollection<IOfficialProviderAdapter> _enabledProviders = providers
            .Where(provider => provider.Descriptor.IsEnabled)
            .ToArray();

        public async Task<ProviderLookupResult> LookupAsync(
            InstalledDriverSnapshot installedDriver,
            CancellationToken cancellationToken)
        {
            var candidates = new List<RecommendationCandidateInput>();
            var failures = new List<ProviderLookupFailure>();

            foreach (var provider in _enabledProviders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ProviderLookupResponse response;
                try
                {
                    response = await provider.LookupAsync(BuildRequest(installedDriver, provider.Descriptor.Code), cancellationToken);
                }
                catch (Exception ex)
                {
                    failures.Add(new ProviderLookupFailure(provider.Descriptor.Code, ex.Message));
                    continue;
                }

                if (!response.IsSuccess)
                {
                    failures.Add(new ProviderLookupFailure(provider.Descriptor.Code, response.FailureReason ?? "Unknown provider failure."));
                    continue;
                }

                foreach (var candidate in response.Candidates)
                {
                    candidates.Add(new RecommendationCandidateInput(response.ProviderCode, candidate));
                }
            }

            return new ProviderLookupResult(candidates, failures);
        }

        private static ProviderLookupRequest BuildRequest(InstalledDriverSnapshot installedDriver, string providerCode)
            => new(
                providerCode,
                installedDriver.DeviceIdentity.InstanceId,
                [installedDriver.HardwareIdentifier.Value],
                installedDriver.DriverVersion,
                OperatingSystemVersion: null,
                DeviceManufacturer: installedDriver.ProviderName,
                DeviceModel: null);
    }

    private sealed record ProviderLookupResult(
        IReadOnlyCollection<RecommendationCandidateInput> Candidates,
        IReadOnlyCollection<ProviderLookupFailure> Failures);

    private sealed record ProviderLookupFailure(
        string ProviderCode,
        string FailureReason);
}
