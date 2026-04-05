using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

    public sealed partial class SettingsSectionViewModel
{
    public async Task LoadSettingsAsync(string defaultDiagnosticLogFolderPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == settings.Reports.DefaultFormat);
        SelectedScanProfile = ScanProfileItems.First(option => option.Value == settings.ScanCoverage.DeviceProfile);
        IsDiagnosticLoggingEnabled = settings.DiagnosticLogging.Enabled;
        CustomDiagnosticLogFolderPath = settings.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        EffectiveDiagnosticLogFolderPath = string.IsNullOrWhiteSpace(settings.DiagnosticLogging.CustomLogsFolderPath)
            ? defaultDiagnosticLogFolderPath
            : settings.DiagnosticLogging.CustomLogsFolderPath.Trim();
        SettingsStatusText = UiStrings.SettingsLoaded;
    }

    public void ApplyStatusMessage(string statusMessage)
    {
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            SettingsStatusText = statusMessage;
        }
    }

    public void ApplyEffectiveDiagnosticFolder(string folderPath)
    {
        EffectiveDiagnosticLogFolderPath = folderPath;
    }

    private async Task SaveSettingsAsync()
    {
        var current = await _settingsRepository.GetAsync(CancellationToken.None);
        var updated = current with
        {
            History = current.History with { MaxEntries = HistoryMaxEntries },
            Reports = current.Reports with { DefaultFormat = SelectedReportFormat.Value },
            WorkflowGuidance = current.WorkflowGuidance with { ShowPostInstallVerificationHints = ShowVerificationHints },
            ScanCoverage = current.ScanCoverage with { DeviceProfile = SelectedScanProfile.Value },
            DiagnosticLogging = current.DiagnosticLogging with
            {
                Enabled = IsDiagnosticLoggingEnabled,
                CustomLogsFolderPath = string.IsNullOrWhiteSpace(CustomDiagnosticLogFolderPath)
                    ? null
                    : CustomDiagnosticLogFolderPath.Trim()
            }
        };

        var normalized = updated.Normalize();
        await _settingsRepository.SaveAsync(normalized, CancellationToken.None);
        CustomDiagnosticLogFolderPath = normalized.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        SettingsStatusText = UiStrings.SettingsSaved;
    }

}
