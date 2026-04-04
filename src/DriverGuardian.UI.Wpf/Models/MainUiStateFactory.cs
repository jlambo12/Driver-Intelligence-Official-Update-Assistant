using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public static class MainUiStateFactory
{
    public static MainUiState CreateFromWorkflowResult(MainScreenWorkflowResult result)
    {
        var status = ResolveStatusText(result);

        return MainUiState.Initial(
            UiStrings.MainWindowTitle,
            status,
            UiStrings.ScanAction) with
        {
            Results = ScanResultsPresentation.FromResult(result)
        };
    }

    private static string ResolveStatusText(MainScreenWorkflowResult result)
    {
        if (result.ScanExecutionStatus == ScanExecutionStatus.Failed)
        {
            return UiStrings.StatusScanFailed;
        }

        if (result.ScanExecutionStatus == ScanExecutionStatus.Partial)
        {
            return UiStrings.StatusScanPartial;
        }

        var hasInsufficientEvidence = result.RecommendationDetails.Any(detail =>
            !detail.HasRecommendation &&
            !string.IsNullOrWhiteSpace(detail.RecommendationReason) &&
            (detail.RecommendationReason.Contains("insufficient evidence", StringComparison.OrdinalIgnoreCase) ||
             detail.RecommendationReason.Contains("недостаточно данных", StringComparison.OrdinalIgnoreCase)));

        if (hasInsufficientEvidence && result.RecommendedCount == 0)
        {
            return $"{UiStrings.StatusScanCompletedNoAction} ({UiStrings.RecommendationStateInsufficientEvidence.ToLowerInvariant()}).";
        }

        if (result.RecommendedCount > 0 && !result.OfficialSourceAction.IsReady)
        {
            return $"{UiStrings.StatusScanCompletedReady} ({UiStrings.RecommendationStateBlocked.ToLowerInvariant()}).";
        }

        return result.RecommendedCount > 0
            ? UiStrings.StatusScanCompletedReady
            : UiStrings.StatusScanCompletedNoAction;
    }
}
