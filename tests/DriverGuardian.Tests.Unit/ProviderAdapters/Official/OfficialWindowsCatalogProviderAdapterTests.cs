using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialWindowsCatalogProviderAdapterTests
{
    [Fact]
    public async Task LookupAsync_ReturnsCandidate_ForSupportedHardwareId()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-1",
                HardwareIds: ["PCI\\VEN_8086&DEV_15F3"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("31.0.101.2125", candidate.CandidateVersion);
        Assert.Equal(SourceTrustLevel.OperatingSystemCatalog, candidate.SourceEvidence.TrustLevel);
        Assert.True(candidate.SourceEvidence.IsOfficialSource);
    }

    [Fact]
    public async Task LookupAsync_ReturnsEmpty_ForUnsupportedHardwareId()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-2",
                HardwareIds: ["PCI\\VEN_9999&DEV_0001"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Unknown",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Null(response.FailureReason);
    }

    [Fact]
    public async Task LookupAsync_ReturnsExplicitFailure_WhenHardwareIdsMissing()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-3",
                HardwareIds: [],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Unknown",
                DeviceModel: null),
            CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(response.Candidates);
        Assert.Contains("hardware id", response.FailureReason, StringComparison.OrdinalIgnoreCase);
    }
}
