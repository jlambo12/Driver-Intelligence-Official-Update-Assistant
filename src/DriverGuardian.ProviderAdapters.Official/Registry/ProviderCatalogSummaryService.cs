using DriverGuardian.Application.Abstractions;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public sealed class ProviderCatalogSummaryService(IProviderRegistry providerRegistry) : IProviderCatalogSummaryService
{
    public Task<int> GetProviderCountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(providerRegistry.GetOfficialProviders().Count);
    }
}
