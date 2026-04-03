using DriverGuardian.Application.Abstractions;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;

namespace DriverGuardian.Application.ProviderCatalog;

public sealed class ProviderCatalogSummaryService(IProviderRegistry providerRegistry) : IProviderCatalogSummaryService
{
    public Task<int> GetProviderCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(providerRegistry.GetOfficialProviders().Count);
    }
}
