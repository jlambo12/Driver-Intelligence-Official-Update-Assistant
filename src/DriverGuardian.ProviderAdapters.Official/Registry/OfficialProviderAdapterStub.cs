using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public sealed class OfficialProviderAdapterStub : IOfficialProviderAdapter
{
    public ProviderDescriptor Descriptor => new(
        Code: "official-stub",
        DisplayName: "Official Sources (Stub)",
        IsEnabled: false,
        OfficialSourceOnly: true,
        Precedence: ProviderPrecedence.PrimaryOem);

    public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        var response = new ProviderLookupResponse(
            ProviderCode: Descriptor.Code,
            IsSuccess: false,
            Candidates: Array.Empty<ProviderCandidate>(),
            FailureReason: "Provider lookup is not implemented in this stage.");

        return Task.FromResult(response);
    }
}
