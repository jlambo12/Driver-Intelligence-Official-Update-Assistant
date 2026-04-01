using DriverGuardian.ProviderAdapters.Abstractions.Models;

namespace DriverGuardian.ProviderAdapters.Abstractions.Contracts;

public interface IOfficialDriverProviderAdapter
{
    string ProviderId { get; }
    Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken);
}
