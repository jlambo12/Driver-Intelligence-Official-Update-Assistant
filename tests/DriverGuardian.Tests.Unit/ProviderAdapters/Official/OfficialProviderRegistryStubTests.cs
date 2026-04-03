using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialProviderRegistryStubTests
{
    [Fact]
    public void GetOfficialProviders_OrdersByPrecedenceThenCode()
    {
        var registry = new OfficialProviderRegistryStub(
        [
            new TestProviderAdapter("b", ProviderPrecedence.PlatformVendor),
            new TestProviderAdapter("a", ProviderPrecedence.PlatformVendor),
            new TestProviderAdapter("oem", ProviderPrecedence.PrimaryOem)
        ]);

        var providers = registry.GetOfficialProviders().ToArray();

        Assert.Collection(providers,
            x => Assert.Equal("oem", x.Code),
            x => Assert.Equal("a", x.Code),
            x => Assert.Equal("b", x.Code));
    }


    [Fact]
    public void DefaultConstructor_RegistersEnabledBaselineProvider()
    {
        var registry = new OfficialProviderRegistryStub();

        var provider = Assert.Single(registry.GetOfficialProviders());
        Assert.Equal("official-baseline", provider.Code);
        Assert.True(provider.IsEnabled);
    }

    [Fact]
    public void TryGetOfficialProvider_IsCaseInsensitive()
    {
        var registry = new OfficialProviderRegistryStub([
            new TestProviderAdapter("OEM-Alpha", ProviderPrecedence.PrimaryOem)
        ]);

        var found = registry.TryGetOfficialProvider("oem-alpha", out var provider);

        Assert.True(found);
        Assert.NotNull(provider);
        Assert.Equal("OEM-Alpha", provider.Code);
    }

    [Fact]
    public void Constructor_ThrowsOnDuplicateCode()
    {
        Assert.Throws<ArgumentException>(() => new OfficialProviderRegistryStub([
            new TestProviderAdapter("dup", ProviderPrecedence.PrimaryOem),
            new TestProviderAdapter("DUP", ProviderPrecedence.PlatformVendor)
        ]));
    }

    private sealed class TestProviderAdapter(string code, ProviderPrecedence precedence) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(
            Code: code,
            DisplayName: code,
            IsEnabled: true,
            OfficialSourceOnly: true,
            Precedence: precedence);

        public Task<DriverGuardian.ProviderAdapters.Abstractions.Lookup.ProviderLookupResponse> LookupAsync(
            DriverGuardian.ProviderAdapters.Abstractions.Lookup.ProviderLookupRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
