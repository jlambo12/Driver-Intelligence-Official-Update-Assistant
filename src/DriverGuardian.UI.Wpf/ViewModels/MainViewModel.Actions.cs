using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private async Task ScanAsync()
    {
        State = State with { StatusText = UiStrings.StatusScanning };
        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);
        ApplyWorkflowResult(result);
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
            State = State with { StatusText = UiStrings.OfficialSourceOpenFailed };
            return;
        }

        State = State with { StatusText = string.Format(UiStrings.OfficialSourceOpenSuccessFormat, approvedUri.Host) };
    }

    private void OpenRecommendationOfficialSource(object? parameter)
    {
        var url = parameter as string;
        if (!SafeOfficialSourceUrlValidator.TryGetSafeHttpsUri(url, out var uri) || uri is null)
        {
            State = State with { StatusText = UiStrings.OfficialSourceUrlUnavailable };
            return;
        }

        if (!_officialSourceLauncher.Open(uri))
        {
            State = State with { StatusText = UiStrings.OfficialSourceOpenFailed };
            return;
        }

        State = State with { StatusText = string.Format(UiStrings.OfficialSourceOpenSuccessFormat, uri.Host) };
    }

    private void ApplyWorkflowResult(MainScreenWorkflowResult result)
    {
        State = MainUiStateFactory.CreateFromWorkflowResult(result);
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
