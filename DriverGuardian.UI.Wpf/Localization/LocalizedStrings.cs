namespace DriverGuardian.UI.Wpf.Localization;

public sealed class LocalizedStrings(ILocalizedTextProvider provider)
{
    public string AppTitle => provider["AppTitle"];
    public string MainHeader => provider["MainHeader"];
    public string ScanButton => provider["ScanButton"];
    public string StatusIdle => provider["StatusIdle"];
    public string StatusScanning => provider["StatusScanning"];
    public string StatusCompleted => provider["StatusCompleted"];
    public string StatusError => provider["StatusError"];
    public string ResultsHeader => provider["ResultsHeader"];
    public string ResultsPlaceholder => provider["ResultsPlaceholder"];
}
