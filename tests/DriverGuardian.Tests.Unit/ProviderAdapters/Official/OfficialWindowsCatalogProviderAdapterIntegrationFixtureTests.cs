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
            var data = new TheoryData<CatalogProviderFixtureCase>();
            foreach (var item in LoadCases())
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
        var success = await EvaluateCaseAsync(fixtureCase);
        Assert.True(success, $"Fixture case '{fixtureCase.Name}' did not match expected provider outcome.");
    }

    [Fact]
    public async Task LookupAsync_FixtureBenchmark_ShouldMeetCoverageBaselineAndTarget()
    {
        var cases = LoadCases();

        var categorized = new Dictionary<string, List<CatalogProviderFixtureCase>>(StringComparer.OrdinalIgnoreCase)
        {
            ["exact"] = [],
            ["normalized"] = [],
            ["vendor-fallback"] = [],
            ["no-match"] = []
        };

        foreach (var fixtureCase in cases)
        {
            categorized[Classify(fixtureCase)].Add(fixtureCase);
        }

        var summary = new Dictionary<string, (int Passed, int Total)>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categorized)
        {
            var passed = 0;
            foreach (var fixtureCase in category.Value)
            {
                if (await EvaluateCaseAsync(fixtureCase))
                {
                    passed++;
                }
            }

            summary[category.Key] = (passed, category.Value.Count);
        }

        var overallTotal = summary.Sum(x => x.Value.Total);
        var overallPassed = summary.Sum(x => x.Value.Passed);
        var overallCoverage = overallTotal == 0 ? 0 : (double)overallPassed / overallTotal;

        Assert.True(overallCoverage >= 0.90,
            $"Overall fixture coverage baseline is below target: {overallPassed}/{overallTotal} ({overallCoverage:P1}).");

        AssertCategoryCoverage(summary, "exact", 0.95);
        AssertCategoryCoverage(summary, "normalized", 0.90);
        AssertCategoryCoverage(summary, "vendor-fallback", 0.85);
        AssertCategoryCoverage(summary, "no-match", 1.00);
    }

    private static void AssertCategoryCoverage(
        IReadOnlyDictionary<string, (int Passed, int Total)> summary,
        string category,
        double minCoverage)
    {
        Assert.True(summary.TryGetValue(category, out var stats),
            $"Missing benchmark category '{category}'.");
        Assert.True(stats.Total > 0,
            $"Category '{category}' must contain at least one fixture case.");

        var coverage = (double)stats.Passed / stats.Total;

        Assert.True(coverage >= minCoverage,
            $"Category '{category}' coverage below target: {stats.Passed}/{stats.Total} ({coverage:P1}), target {minCoverage:P0}.");
    }

    private static async Task<bool> EvaluateCaseAsync(CatalogProviderFixtureCase fixtureCase)
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

        if (!response.IsSuccess)
        {
            return false;
        }

        if (fixtureCase.ExpectedCandidateVersion is null)
        {
            return response.Candidates.Count == 0;
        }

        if (response.Candidates.Count != 1)
        {
            return false;
        }

        var candidate = response.Candidates[0];

        if (!string.Equals(fixtureCase.ExpectedCandidateVersion, candidate.CandidateVersion, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Enum.TryParse<CompatibilityConfidence>(fixtureCase.ExpectedConfidence, ignoreCase: true, out var expectedConfidence) ||
            expectedConfidence != candidate.CompatibilityConfidence)
        {
            return false;
        }

        return candidate.SourceEvidence.EvidenceNote.Contains(
            fixtureCase.ExpectedEvidenceToken!,
            StringComparison.OrdinalIgnoreCase);
    }

    private static List<CatalogProviderFixtureCase> LoadCases()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, FixtureRelativePath);
        var json = File.ReadAllText(fixturePath);

        return JsonSerializer.Deserialize<List<CatalogProviderFixtureCase>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private static string Classify(CatalogProviderFixtureCase fixtureCase)
    {
        if (fixtureCase.ExpectedCandidateVersion is null)
        {
            return "no-match";
        }

        if (string.Equals(fixtureCase.ExpectedEvidenceToken, "exact", StringComparison.OrdinalIgnoreCase))
        {
            return "exact";
        }

        if (string.Equals(fixtureCase.ExpectedEvidenceToken, "normalized", StringComparison.OrdinalIgnoreCase))
        {
            return "normalized";
        }

        return "vendor-fallback";
    }

    public sealed record CatalogProviderFixtureCase(
        string Name,
        IReadOnlyCollection<string> HardwareIds,
        string? ExpectedCandidateVersion,
        string? ExpectedConfidence,
        string? ExpectedEvidenceToken);
}
