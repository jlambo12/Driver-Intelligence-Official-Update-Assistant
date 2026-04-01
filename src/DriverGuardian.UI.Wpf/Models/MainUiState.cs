namespace DriverGuardian.UI.Wpf.Models;

public sealed record MainUiState(
    string TitleText,
    string StatusText,
    string ScanButtonText,
    string ExportReportButtonText,
    string ReportSectionTitle,
    string ReportPreviewText,
    ScanResultsPresentation Results)
{
    public static MainUiState Initial(
        string titleText,
        string statusText,
        string scanButtonText,
        string exportReportButtonText,
        string reportSectionTitle,
        string reportPreviewPlaceholder) =>
        new(titleText, statusText, scanButtonText, exportReportButtonText, reportSectionTitle, reportPreviewPlaceholder, ScanResultsPresentation.Empty());
}
