using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

/// <summary>
/// Runtime-enabled baseline official provider.
/// It executes lookup requests in production wiring but returns no candidates
/// until a real external source integration is available.
/// </summary>
public sealed class OfficialProviderAdapterBaseline : IOfficialProviderAdapter
{
    public ProviderDescriptor Descriptor => new(
        Code: "official-baseline",
        DisplayName: "Official Sources (Baseline)",
        IsEnabled: true,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PlatformVendor);

    public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: true,
            Candidates: Array.Empty<ProviderCandidate>(),
            FailureReason: null);

        return Task.FromResult(response);
    }
}
