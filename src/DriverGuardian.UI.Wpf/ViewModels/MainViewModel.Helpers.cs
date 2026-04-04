using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed partial class MainViewModel
{
    private bool TryGetApprovedOfficialSourceUri(out Uri? uri)
    {
        uri = null;

        if (!State.Results.HasScanData || string.IsNullOrWhiteSpace(_lastApprovedOfficialSourceUrl))
        {
            return false;
        }

        return SafeOfficialSourceUrlValidator.TryGetSafeHttpsUri(_lastApprovedOfficialSourceUrl, out uri);
    }

    private void SyncEffectiveDiagnosticFolder()
    {
        var effective = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(SettingsSection.CustomDiagnosticLogFolderPath);
        SettingsSection.ApplyEffectiveDiagnosticFolder(effective);
    }
}
