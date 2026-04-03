using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public static class OfficialProviderRuntimeFactory
{
    public static IReadOnlyCollection<IOfficialProviderAdapter> CreateRuntimeProviders()
        =>
        [
            new OfficialWindowsCatalogProviderAdapter(),
            new OfficialProviderAdapterBaseline()
        ];
}
