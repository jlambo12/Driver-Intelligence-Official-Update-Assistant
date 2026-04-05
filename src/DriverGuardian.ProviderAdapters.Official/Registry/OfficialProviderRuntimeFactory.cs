using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public static class OfficialProviderRuntimeFactory
{
    public static IReadOnlyCollection<IOfficialProviderAdapter> CreateRuntimeProviders()
    {
        IOfficialProviderAdapter[] baseProviders =
        [
            new OfficialOemSupportProviderAdapter(),
            new OfficialWindowsCatalogProviderAdapter()
        ];

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
