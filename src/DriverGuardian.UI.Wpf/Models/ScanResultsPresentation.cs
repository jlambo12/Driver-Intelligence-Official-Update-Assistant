using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record ScanResultsPresentation(
    string ScanSummary,
    string DiscoveryAndInspectionSummary,
    string RecommendationSummary,
    string ManualHandoffSummary,
    string VerificationSummary)
{
    public static ScanResultsPresentation Empty() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

    public static ScanResultsPresentation FromResult(MainScreenWorkflowResult result)
    {
        return new ScanResultsPresentation(
            ScanSummary: string.Format(UiStrings.ScanSummaryFormat, result.ScanSessionId, result.UiCulture, result.ProviderCount),
            DiscoveryAndInspectionSummary: string.Format(
                UiStrings.DiscoveryInspectionSummaryFormat,
                result.DiscoveredDeviceCount,
                result.InspectedDriverCount),
            RecommendationSummary: string.Format(
                UiStrings.RecommendationSummaryFormat,
                result.RecommendedCount,
                result.NotRecommendedCount),
            ManualHandoffSummary: string.Format(
                UiStrings.ManualHandoffSummaryFormat,
                result.ManualHandoffReadyCount,
                result.ManualHandoffUserActionCount),
            VerificationSummary: string.Format(UiStrings.VerificationSummaryFormat, result.VerificationSummary));
    }
}
