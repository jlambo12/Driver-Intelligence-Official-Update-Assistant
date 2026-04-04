using DriverGuardian.Application.MainScreen;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private async Task ApplyPreviewScenarioAsync()
    {
        if (_previewWorkflow is null)
        {
            return;
        }

        var scenario = SelectedPreviewScenario ?? AvailablePreviewScenarios.First();
        _previewWorkflow.SelectScenario(scenario.Id);

        if (scenario.Id == PreviewScenarioId.FirstRunPreScan)
        {
            HistorySection.RecentHistory = Array.Empty<RecentHistoryPresentation>();
            ReportSection.ClearPayload("driverguardian-preview-first-run");
            WorkflowSection.ShowSecondaryRecommendations = false;
            State = MainUiState.Initial(
                UiStrings.PreviewWindowTitle,
                string.Format(UiStrings.PreviewModeStatusFormat, scenario.DisplayName),
                UiStrings.PreviewApplyScenarioAction);
            return;
        }

        var result = await _previewWorkflow.RunScanAsync(CancellationToken.None);
        ApplyWorkflowResult(result, isPreview: true, scenarioName: scenario.DisplayName);
    }

    private IReadOnlyList<PreviewScenarioOption> BuildPreviewOptions()
    {
        if (_previewWorkflow is null)
        {
            return Array.Empty<PreviewScenarioOption>();
        }

        return _previewWorkflow.AvailableScenarios
            .Select(id => new PreviewScenarioOption(id, GetScenarioName(id)))
            .ToArray();
    }

    private static string GetScenarioName(PreviewScenarioId scenarioId)
        => scenarioId switch
        {
            PreviewScenarioId.FirstRunPreScan => UiStrings.PreviewScenarioFirstRun,
            PreviewScenarioId.NoActionableRecommendation => UiStrings.PreviewScenarioNoAction,
            PreviewScenarioId.RecommendationWithLimitedEvidence => UiStrings.PreviewScenarioLimitedEvidence,
            PreviewScenarioId.RecommendationReadyForManualAction => UiStrings.PreviewScenarioManualReady,
            PreviewScenarioId.VerificationReturnGuidance => UiStrings.PreviewScenarioVerificationReturn,
            PreviewScenarioId.PopulatedHistoryAndExport => UiStrings.PreviewScenarioHistoryExport,
            _ => UiStrings.PreviewScenarioFirstRun
        };
}
