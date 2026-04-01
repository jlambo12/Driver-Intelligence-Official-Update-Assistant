namespace DriverGuardian.UI.Wpf.Models;

public sealed record MainUiState(string TitleText, string StatusText, string ScanButtonText, string LastScanSummary)
{
    public static MainUiState Initial(string titleText, string statusText, string scanButtonText) =>
        new(titleText, statusText, scanButtonText, string.Empty);
}
