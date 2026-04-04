using System.Text.Json;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Official.Registry;

namespace DriverGuardian.Tests.Unit.ProviderAdapters.Official;

public sealed class OfficialWindowsCatalogProviderAdapterIntegrationFixtureTests
{
    private const string FixtureRelativePath = "ProviderAdapters/Official/Fixtures/windows-catalog-provider-cases.json";

    public static TheoryData<CatalogProviderFixtureCase> Cases
    {
        get
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, FixtureRelativePath);
            var json = File.ReadAllText(fixturePath);
            var items = JsonSerializer.Deserialize<List<CatalogProviderFixtureCase>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

            var data = new TheoryData<CatalogProviderFixtureCase>();
            foreach (var item in items)
            {
                data.Add(item);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task LookupAsync_ShouldMatchFixtureExpectations(CatalogProviderFixtureCase fixtureCase)
    {
        var adapter = new OfficialWindowsCatalogProviderAdapter();

        var response = await adapter.LookupAsync(
            new ProviderLookupRequest(
                ProviderCode: adapter.Descriptor.Code,
                DeviceInstanceId: $"fixture-{fixtureCase.Name}",
                HardwareIds: fixtureCase.HardwareIds,
                InstalledDriverVersion: "1.0.0",
                OperatingSystemVersion: null,
                DeviceManufacturer: "Fixture",
                DeviceModel: null),
            CancellationToken.None);

        Assert.True(response.IsSuccess);

        if (fixtureCase.ExpectedCandidateVersion is null)
        {
            Assert.Empty(response.Candidates);
            return;
        }

        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(fixtureCase.ExpectedCandidateVersion, candidate.CandidateVersion);
        Assert.Equal(
            Enum.Parse<CompatibilityConfidence>(fixtureCase.ExpectedConfidence!, ignoreCase: true),
            candidate.CompatibilityConfidence);
        Assert.Contains(fixtureCase.ExpectedEvidenceToken!, candidate.SourceEvidence.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    public sealed record CatalogProviderFixtureCase(
        string Name,
        IReadOnlyCollection<string> HardwareIds,
        string? ExpectedCandidateVersion,
        string? ExpectedConfidence,
        string? ExpectedEvidenceToken);
}
