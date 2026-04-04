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

        if (!Uri.TryCreate(_lastApprovedOfficialSourceUrl, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!parsed.IsAbsoluteUri ||
            !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parsed.Host) ||
            parsed.IsLoopback ||
            !string.IsNullOrEmpty(parsed.UserInfo))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private void SyncEffectiveDiagnosticFolder()
    {
        var effective = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(SettingsSection.CustomDiagnosticLogFolderPath);
        SettingsSection.ApplyEffectiveDiagnosticFolder(effective);
    }
}
