using DriverGuardian.Application.Abstractions;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public static class MainUiStateFactory
{
    public static MainUiState CreateFromWorkflowResult(MainScreenWorkflowResult result, bool isPreview, string? scenarioName)
    {
        var status = result.ScanExecutionStatus switch
        {
            ScanExecutionStatus.Failed => "Сканирование завершилось с ошибкой: данные недостоверны.",
            ScanExecutionStatus.Partial => "Сканирование частично завершено: часть данных может отсутствовать.",
            _ => result.RecommendedCount > 0
                ? UiStrings.StatusScanCompletedReady
                : UiStrings.StatusScanCompletedNoAction
        };

        if (isPreview)
        {
            status = string.Format(UiStrings.PreviewModeStatusFormat, scenarioName ?? string.Empty);
        }

        return MainUiState.Initial(
            isPreview ? UiStrings.PreviewWindowTitle : UiStrings.MainWindowTitle,
            status,
            isPreview ? UiStrings.PreviewApplyScenarioAction : UiStrings.ScanAction) with
        {
            Results = ScanResultsPresentation.FromResult(result)
        };
    }
}
