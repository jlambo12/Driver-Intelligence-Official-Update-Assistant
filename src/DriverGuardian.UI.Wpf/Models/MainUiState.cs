namespace DriverGuardian.UI.Wpf.Models;

public sealed record MainUiState(
    string TitleText,
    string StatusText,
    string ScanButtonText,
    ScanResultsPresentation Results)
{
    public static MainUiState Initial(string titleText, string statusText, string scanButtonText) =>
        new(titleText, statusText, scanButtonText, ScanResultsPresentation.Empty());
}
