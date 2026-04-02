using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.Tests.Unit.UI;

public sealed class ScanResultsPresentationTests
{
    [Fact]
    public void FromResult_ShouldKeepPrimaryRecommendationsCapped_AndExposeSecondaryRecommendations()
    {
        var details = Enumerable.Range(1, 8)
            .Select(index => new RecommendationDetailResult(
                DeviceDisplayName: $"Устройство {index}",
                DeviceId: $"PCI\\VEN_1234&DEV_{index:0000}",
                HasRecommendation: true,
                RecommendationReason: "Проверить новую версию",
                InstalledVersion: "1.0.0",
                InstalledProvider: "Test",
                RecommendedVersion: "2.0.0",
                ManualHandoffReady: true,
                ManualActionRequired: true,
                VerificationAvailable: true,
                VerificationStatus: "ожидается"))
            .ToArray();

        var result = BuildResult(details);

        var presentation = ScanResultsPresentation.FromResult(result);

        Assert.Equal(6, presentation.RecommendationDetails.Count);
        Assert.Equal(2, presentation.SecondaryRecommendationDetails.Count);
        Assert.True(presentation.HasSecondaryRecommendations);
        Assert.True(presentation.ShowSecondaryRecommendationSummary);
        Assert.Equal(2, presentation.SecondaryRecommendationCount);
        Assert.False(string.IsNullOrWhiteSpace(presentation.SecondaryRecommendationToggleText));
    }

    private static MainScreenWorkflowResult BuildResult(IReadOnlyCollection<RecommendationDetailResult> details)
    {
        return new MainScreenWorkflowResult(
            DiscoveredDeviceCount: 8,
            InspectedDriverCount: 8,
            RecommendedCount: 8,
            NotRecommendedCount: 0,
            ProviderCount: 1,
            ManualHandoffReadyCount: 8,
            ManualHandoffUserActionCount: 8,
            VerificationSummary: "ожидается",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.NewGuid(),
            ReportExportPayload: new ReportExportPayload("test", "plain", "md"),
            RecommendationDetails: details,
            OfficialSourceAction: new OpenOfficialSourceActionResult(true, "ready", "https://example.com", null),
            RecentHistory: Array.Empty<RecentHistoryEntryResult>());
    }
}
