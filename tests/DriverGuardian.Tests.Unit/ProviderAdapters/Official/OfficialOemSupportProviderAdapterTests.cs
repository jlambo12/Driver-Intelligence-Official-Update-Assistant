using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialOemSupportProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_ReturnsOemCandidate_ForKnownManufacturer()
    {
        var adapter = new OfficialOemSupportProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-1",
                HardwareIds: ["PCI\\VEN_1022&DEV_15E3"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: "Windows 11 23H2",
                DeviceManufacturer: "Dell Inc.",
                DeviceModel: "Precision 3580"),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(SourceTrustLevel.OemSupportPortal, candidate.SourceEvidence.TrustLevel);
        Assert.Contains("dell.com", candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Precision%203580", candidate.SourceEvidence.SourceUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_ForUnknownManufacturer()
    {
        var adapter = new OfficialOemSupportProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-2",
                HardwareIds: ["PCI\\VEN_9999&DEV_0001"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Unknown Vendor",
                DeviceModel: "Model X"),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
    }

    [Theory]
    [InlineData("Samsung Electronics", "samsung.com")]
    [InlineData("Huawei", "huawei.com")]
    [InlineData("LG Electronics", "lg.com")]
    [InlineData("Toshiba", "dynabook.com")]
    public async Task LookupAsync_ReturnsOemCandidate_ForAdditionalGlobalManufacturers(string manufacturer, string expectedHostToken)
    {
        var adapter = new OfficialOemSupportProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-3",
                HardwareIds: ["PCI\\VEN_8086&DEV_1234"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: "Windows 11 23H2",
                DeviceManufacturer: manufacturer,
                DeviceModel: "TestModel"),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Contains(expectedHostToken, candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase);
    }
}
