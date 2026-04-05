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
    public async Task LookupAsync_UsesNormalizedPciHardwareId_WhenSubsystemSuffixPresent()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-4",
                HardwareIds: ["PCI\\VEN_8086&DEV_15F3&SUBSYS_00000000&REV_10"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("31.0.101.2125", candidate.CandidateVersion);
        Assert.Equal(HardwareIdMatchStrength.NormalizedHardwareId, candidate.MatchStrength);
        Assert.Contains("normalized", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_UsesNormalizedUsbHardwareId_WhenRevisionSuffixPresent()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-5",
                HardwareIds: ["USB\\VID_0BDA&PID_8153&REV_3000"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Realtek",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("10.63.20.1028", candidate.CandidateVersion);
        Assert.Equal(HardwareIdMatchStrength.NormalizedHardwareId, candidate.MatchStrength);
        Assert.Contains("normalized", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task LookupAsync_UsesVendorCompatibleFallback_WhenExactMatchIsMissing()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-6",
                HardwareIds: ["PCI\\VEN_8086&DEV_ABCD"],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(CompatibilityConfidence.Unknown, candidate.CompatibilityConfidence);
        Assert.Equal(HardwareIdMatchStrength.VendorFallback, candidate.MatchStrength);
        Assert.Contains("vendor-fallback", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_PrefersExactMatchOverCompatibleFallback_WhenBothExist()
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: "DEV-7",
                HardwareIds:
                [
                    "PCI\\VEN_8086&DEV_ABCD",
                    "PCI\\VEN_8086&DEV_15F3"
                ],
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Intel",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal("31.0.101.2125", candidate.CandidateVersion);
        Assert.Equal(CompatibilityConfidence.Medium, candidate.CompatibilityConfidence);
        Assert.Equal(HardwareIdMatchStrength.ExactHardwareId, candidate.MatchStrength);
        Assert.Contains("exact", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupAsync_UsesExternalCatalogJson_AsUpdateableSource_WhenProvided()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, """
[
  {
    "hardwareId": "PCI\\VEN_AAAA&DEV_BBBB",
    "driverIdentifier": "external-driver",
    "candidateVersion": "99.0.0.1",
    "sourceUri": "https://www.catalog.update.microsoft.com/Search.aspx?q=PCI%5CVEN_AAAA%26DEV_BBBB",
    "publisherName": "External Catalog",
    "evidenceNote": "Loaded from integration test external catalog source."
  }
]
""");

            var adapter = new OfficialWindowsCatalogProviderAdapter(path);

            var response = await adapter.LookupAsync(
                new ProviderLookupRequest(
                    ProviderCode: adapter.Descriptor.Code,
                    DeviceInstanceId: "DEV-EXT",
                    HardwareIds: [@"PCI\VEN_AAAA&DEV_BBBB"],
                    InstalledDriverVersion: "1.0.0",
                    OperatingSystemVersion: null,
                    DeviceManufacturer: "Test",
                    DeviceModel: null),
                CancellationToken.None);

            var candidate = Assert.Single(response.Candidates);
            Assert.Equal("99.0.0.1", candidate.CandidateVersion);
            Assert.Equal(CompatibilityConfidence.High, candidate.CompatibilityConfidence);
            Assert.Contains("external-json", candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

}
