using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialProviderAdapterBaselineTests
{
    [Fact]
    public async Task LookupAsync_ReturnsSuccessfulEmptyResult()
    {
        var adapter = new OfficialProviderAdapterBaseline();

        var response = await adapter.LookupAsync(
            new DriverGuardian.ProviderAdapters.Abstractions.Lookup.ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-1",
                HardwareIds: ["PCI\\VEN_1234&DEV_5678"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Contoso",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(adapter.Descriptor.IsEnabled);
        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Null(response.FailureReason);
    }
}
