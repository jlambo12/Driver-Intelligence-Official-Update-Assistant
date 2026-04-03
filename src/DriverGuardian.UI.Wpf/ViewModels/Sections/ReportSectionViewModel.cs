using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class ReportSectionViewModel : INotifyPropertyChanged
{
    private readonly IReportFileSaveService _reportFileSaveService;
    private ReportFormatOption _selectedReportFormat;
    private string _reportExportStatusText;
    private string _reportFileNameBase;
    private string _reportPlainTextContent;
    private string _reportMarkdownContent;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public ReportSectionViewModel(IReportFileSaveService reportFileSaveService)
    {
        _reportFileSaveService = reportFileSaveService;
        _selectedReportFormat = ReportFormatItems[0];
        _reportExportStatusText = UiStrings.ReportExportStatusNoData;
        _reportFileNameBase = "driverguardian-report";
        _reportPlainTextContent = string.Empty;
        _reportMarkdownContent = string.Empty;
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ExportReportCommand { get; }

    public IReadOnlyList<ReportFormatOption> AvailableReportFormats => ReportFormatItems;

    public ReportFormatOption SelectedReportFormat
    {
        get => _selectedReportFormat;
        set
        {
            if (_selectedReportFormat.Equals(value))
            {
                return;
            }

            _selectedReportFormat = value;
            OnPropertyChanged();
        }
    }

    public string ReportExportStatusText
    {
        get => _reportExportStatusText;
        private set
        {
            _reportExportStatusText = value;
            OnPropertyChanged();
        }
    }

    public void ApplySettingsReportFormat(ShareableReportFormat format)
    {
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == format);
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

    private async Task ExportReportAsync()
    {
        await Task.Yield();

        if (string.IsNullOrWhiteSpace(_reportPlainTextContent))
        {
            ReportExportStatusText = UiStrings.ReportExportStatusNoData;
            return;
        }

        var isMarkdown = SelectedReportFormat.Value == ShareableReportFormat.Markdown;
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
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
