using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public static class MainUiStateFactory
{
    public static MainUiState CreateFromWorkflowResult(MainScreenWorkflowResult result)
    {
        var status = result.ScanExecutionStatus switch
        {
            ScanExecutionStatus.Failed => UiStrings.StatusScanFailed,
            ScanExecutionStatus.Partial => UiStrings.StatusScanPartial,
            _ => result.RecommendedCount > 0
                ? UiStrings.StatusScanCompletedReady
                : UiStrings.StatusScanCompletedNoAction
        };

        return MainUiState.Initial(
            UiStrings.MainWindowTitle,
            status,
            UiStrings.ScanAction) with
        {
            Results = ScanResultsPresentation.FromResult(result)
        };
    }
}
