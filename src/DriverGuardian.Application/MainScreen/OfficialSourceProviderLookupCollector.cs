using DriverGuardian.Domain.Drivers;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Application.MainScreen;

internal sealed class OfficialSourceProviderLookupCollector
{
    public async Task<OfficialSourceProviderLookupCollection> CollectAsync(
        IReadOnlyCollection<IOfficialProviderAdapter> providers,
        InstalledDriverSnapshot targetDriver,
        CancellationToken cancellationToken)
    {
        var failures = new List<OfficialSourceProviderFailure>();
        var policyCandidates = new List<OfficialSourcePolicyCandidate>();

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProviderLookupResponse response;
            try
            {
                response = await provider.LookupAsync(
                    BuildRequest(provider.Descriptor.Code, targetDriver),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                failures.Add(new OfficialSourceProviderFailure(provider.Descriptor.Code, ex.Message, ex.GetType().Name));
                continue;
            }

            if (!response.IsSuccess)
            {
                failures.Add(new OfficialSourceProviderFailure(provider.Descriptor.Code, response.FailureReason ?? "Unknown provider failure.", null));
                continue;
            }

            foreach (var candidate in response.Candidates)
            {
                if (OfficialSourcePolicyCandidateFactory.TryBuild(provider.Descriptor.Code, candidate, out var policyCandidate))
                {
                    policyCandidates.Add(policyCandidate);
                }
            }
        }

        return new OfficialSourceProviderLookupCollection(policyCandidates, failures);
    }

    private static ProviderLookupRequest BuildRequest(string providerCode, InstalledDriverSnapshot targetDriver)
        => new(
            providerCode,
            targetDriver.DeviceIdentity.InstanceId,
            [targetDriver.HardwareIdentifier.Value],
            targetDriver.DriverVersion,
            OperatingSystemVersion: null,
            DeviceManufacturer: targetDriver.ProviderName,
            DeviceModel: null);
}

internal sealed record OfficialSourceProviderLookupCollection(
    IReadOnlyCollection<OfficialSourcePolicyCandidate> PolicyCandidates,
    IReadOnlyCollection<OfficialSourceProviderFailure> Failures);
