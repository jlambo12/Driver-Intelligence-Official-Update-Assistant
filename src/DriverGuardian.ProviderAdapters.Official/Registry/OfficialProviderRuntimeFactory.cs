using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public static class OfficialProviderRuntimeFactory
{
    public static IReadOnlyCollection<IOfficialProviderAdapter> CreateRuntimeProviders()
    {
        var catalogSource = new SnapshotWindowsCatalogDataSource();

        var baseProviders = new IOfficialProviderAdapter[]
        {
            new OfficialOemSupportProviderAdapter(),
            new OfficialWindowsCatalogProviderAdapter(catalogSource)
        };

        return baseProviders
            .Select(provider => new ResilientOfficialProviderAdapter(
                provider,
                timeout: TimeSpan.FromSeconds(3),
                maxTransientRetries: 1,
                failureThreshold: 3,
                breakDuration: TimeSpan.FromSeconds(30)))
            .Cast<IOfficialProviderAdapter>()
            .ToArray();
    }
}
