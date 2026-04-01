using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public sealed class OfficialProviderRegistryStub : IProviderRegistry
{
    public IReadOnlyCollection<ProviderDescriptor> GetProviders()
    {
        return
        [
            new ProviderDescriptor("official-stub", "Official Sources (Stub)", IsEnabled: false)
        ];
    }
}
