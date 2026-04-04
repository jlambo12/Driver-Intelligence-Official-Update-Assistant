using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private async Task ScanAsync()
    {
        if (IsPreviewMode)
        {
            await ApplyPreviewScenarioAsync();
            return;
        }

        State = State with { StatusText = UiStrings.StatusScanning };
        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);
        ApplyWorkflowResult(result, isPreview: false);
    }

    private async Task ExportReportAsync()
    {
        await ReportSection.ExportAsync(SettingsSection.SelectedReportFormat.Value);
    }

    private async Task OpenDiagnosticLogsFolderAsync()
    {
        await Task.Yield();

        var opened = _diagnosticLogsFolderService.OpenFolder(SettingsSection.EffectiveDiagnosticLogFolderPath);
        if (!opened)
        {
            SettingsSection.ApplyStatusMessage(UiStrings.SettingsLogsFolderOpenFailed);
        }
    }

    private async Task OpenOfficialSourceAsync()
    {
        await Task.Yield();

        if (!TryGetApprovedOfficialSourceUri(out var approvedUri) || approvedUri is null)
        {
            State = State with { StatusText = OpenOfficialSourceBlockReason };
            return;
        }

        if (!_officialSourceLauncher.Open(approvedUri))
        {
            State = State with { StatusText = "Не удалось открыть официальный источник. Проверьте браузер по умолчанию." };
            return;
        }

        State = State with { StatusText = $"Открыт официальный источник: {approvedUri.Host}" };
    }

    private void ApplyWorkflowResult(MainScreenWorkflowResult result, bool isPreview, string? scenarioName = null)
    {
        State = MainUiStateFactory.CreateFromWorkflowResult(result, isPreview, scenarioName);
        WorkflowSection.ShowSecondaryRecommendations = false;
        HistorySection.RecentHistory = RecentHistoryPresentation.FromResults(result.RecentHistory);
        _lastApprovedOfficialSourceUrl = result.OfficialSourceAction.ApprovedOfficialSourceUrl;
        ReportSection.ApplyWorkflowPayload(result.ReportExportPayload);
        OnPropertyChanged(nameof(CanOpenOfficialSource));
        OnPropertyChanged(nameof(OpenOfficialSourceBlockReason));

        if (OpenOfficialSourceCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
