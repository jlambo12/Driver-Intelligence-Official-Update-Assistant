using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialProviderRuntimeFactoryTests
{
    [Fact]
    public void CreateRuntimeProviders_IncludesRealProviderInEnabledRuntimePath()
    {
        var providers = OfficialProviderRuntimeFactory.CreateRuntimeProviders();

        Assert.Contains(providers, x => x is OfficialWindowsCatalogProviderAdapter && x.Descriptor.IsEnabled);
        Assert.Contains(providers, x => x is OfficialProviderAdapterBaseline && x.Descriptor.IsEnabled);
    }
}
