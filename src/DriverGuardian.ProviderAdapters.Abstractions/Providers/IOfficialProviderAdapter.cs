using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;

namespace DriverGuardian.ProviderAdapters.Abstractions.Providers;

public interface IOfficialProviderAdapter
{
    ProviderDescriptor Descriptor { get; }

    Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken);
}
