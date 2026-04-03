using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Application.OfficialSources;

public sealed class OfficialSourceResolutionService(
    IEnumerable<IOfficialProviderAdapter> providers,
    OpenOfficialSourceActionEvaluator openOfficialSourceActionEvaluator) : IOfficialSourceResolutionService
{
    private readonly IReadOnlyCollection<IOfficialProviderAdapter> _enabledProviders = providers
        .Where(provider => provider.Descriptor.IsEnabled)
        .OrderBy(provider => provider.Descriptor.Precedence)
        .ThenBy(provider => provider.Descriptor.Code, StringComparer.Ordinal)
        .ToArray();

    public async Task<OfficialSourceResolutionResult> ResolveAsync(
        OfficialSourceResolutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        OfficialSourceResolutionResult? insufficientEvidence = null;

        foreach (var provider in _enabledProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProviderLookupResponse response;
            try
            {
                response = await provider.LookupAsync(BuildLookupRequest(request, provider.Descriptor.Code), cancellationToken);
            }
            catch
            {
                continue;
            }

            if (!response.IsSuccess)
            {
                continue;
            }

            foreach (var candidate in response.Candidates)
            {
                var officialSourceUri = ResolveOfficialSourceUri(candidate);
                var decision = openOfficialSourceActionEvaluator.Evaluate(
                    new OpenOfficialSourceActionRequest(
                        response.ProviderCode,
                        candidate.DriverIdentifier,
                        candidate.SourceEvidence,
                        officialSourceUri));

                var result = new OfficialSourceResolutionResult(decision, candidate.SourceEvidence, officialSourceUri);

                if (decision.ResolutionOutcome != OfficialSourceResolutionOutcome.InsufficientEvidence)
                {
                    return result;
                }

                insufficientEvidence ??= result;
            }
        }

        return insufficientEvidence
            ?? new OfficialSourceResolutionResult(
                new OpenOfficialSourceActionDecision(
                    OpenOfficialSourceActionOutcome.InsufficientEvidence,
                    OfficialSourceResolutionOutcome.InsufficientEvidence,
                    null,
                    [
                        new OpenOfficialSourceBlocker(
                            OpenOfficialSourceBlockedReason.SourceTrustUnverified,
                            "No official-source candidates were returned by enabled providers.")
                    ]),
                null,
                null);
    }

    private static ProviderLookupRequest BuildLookupRequest(OfficialSourceResolutionRequest request, string providerCode)
        => new(
            providerCode,
            request.DeviceInstanceId,
            [request.HardwareId],
            request.InstalledDriverVersion,
            OperatingSystemVersion: null,
            DeviceManufacturer: request.DeviceManufacturer,
            DeviceModel: null);

    private static Uri? ResolveOfficialSourceUri(ProviderCandidate candidate)
        => candidate.DownloadUri ?? candidate.SourceEvidence.SourceUri;
}
