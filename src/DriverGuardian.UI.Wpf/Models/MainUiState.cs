namespace DriverGuardian.UI.Wpf.Models;

public sealed record MainUiState(
    string TitleText,
    string StatusText,
    string ScanButtonText,
    string VerifyReturnButtonText,
    bool IsManualInstallConfirmed,
    ScanResultsPresentation Results)
{
    public static MainUiState Initial(string titleText, string statusText, string scanButtonText, string verifyReturnButtonText) =>
        new(titleText, statusText, scanButtonText, verifyReturnButtonText, false, ScanResultsPresentation.Empty());
}
