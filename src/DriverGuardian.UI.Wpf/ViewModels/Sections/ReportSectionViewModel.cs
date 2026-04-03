using System.ComponentModel;
using System.Runtime.CompilerServices;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class ReportSectionViewModel : INotifyPropertyChanged
{
    private readonly IReportFileSaveService _reportFileSaveService;
    private string _reportExportStatusText;
    private string _reportFileNameBase;
    private string _reportPlainTextContent;
    private string _reportMarkdownContent;

    public ReportSectionViewModel(IReportFileSaveService reportFileSaveService)
    {
        _reportFileSaveService = reportFileSaveService;
        _reportExportStatusText = UiStrings.ReportExportStatusNoData;
        _reportFileNameBase = "driverguardian-report";
        _reportPlainTextContent = string.Empty;
        _reportMarkdownContent = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ReportExportStatusText
    {
        get => _reportExportStatusText;
        private set
        {
            _reportExportStatusText = value;
            OnPropertyChanged();
        }
    }

    public void ApplyWorkflowPayload(ReportExportPayload payload)
    {
        _reportFileNameBase = payload.FileNameBase;
        _reportPlainTextContent = payload.PlainTextContent;
        _reportMarkdownContent = payload.MarkdownContent;
        ReportExportStatusText = string.IsNullOrWhiteSpace(_reportPlainTextContent)
            ? UiStrings.ReportExportStatusNoData
            : UiStrings.ReportExportStatusReady;
    }

    public void ClearPayload(string fileNameBase)
    {
        _reportFileNameBase = fileNameBase;
        _reportPlainTextContent = string.Empty;
        _reportMarkdownContent = string.Empty;
        ReportExportStatusText = UiStrings.ReportExportStatusNoData;
    }

    public Task ExportAsync(ShareableReportFormat reportFormat)
    {
        if (string.IsNullOrWhiteSpace(_reportPlainTextContent))
        {
            ReportExportStatusText = UiStrings.ReportExportStatusNoData;
            return Task.CompletedTask;
        }

        var isMarkdown = reportFormat == ShareableReportFormat.Markdown;
        var extension = isMarkdown ? ".md" : ".txt";
        var filter = isMarkdown ? UiStrings.ReportExportMarkdownFilter : UiStrings.ReportExportTextFilter;
        var content = isMarkdown ? _reportMarkdownContent : _reportPlainTextContent;

        var saveResult = _reportFileSaveService.Save(_reportFileNameBase, extension, filter, content);
        ReportExportStatusText = saveResult switch
        {
            ReportFileSaveResult.Saved => UiStrings.ReportExportStatusSaved,
            ReportFileSaveResult.CanceledByUser => UiStrings.ReportExportStatusCanceled,
            ReportFileSaveResult.FailedToWrite => UiStrings.ReportExportStatusSaveFailed,
            _ => UiStrings.ReportExportStatusSaveFailed
        };

        return Task.CompletedTask;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
