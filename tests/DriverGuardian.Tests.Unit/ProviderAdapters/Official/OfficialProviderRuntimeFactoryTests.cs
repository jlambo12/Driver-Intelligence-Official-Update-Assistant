using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialProviderRuntimeFactoryTests
{
    [Fact]
    public void CreateRuntimeProviders_IncludesRealProviderInEnabledRuntimePath()
    {
        var providers = OfficialProviderRuntimeFactory.CreateRuntimeProviders();

        Assert.All(providers, provider => Assert.IsType<ResilientOfficialProviderAdapter>(provider));
        Assert.Contains(providers, x => x.Descriptor.Code == "oem-support-portal" && x.Descriptor.IsEnabled);
        Assert.Contains(providers, x => x.Descriptor.Code == "windows-update-catalog" && x.Descriptor.IsEnabled);
        Assert.DoesNotContain(providers, x => x.Descriptor.Code == "official-baseline");
    }
}
