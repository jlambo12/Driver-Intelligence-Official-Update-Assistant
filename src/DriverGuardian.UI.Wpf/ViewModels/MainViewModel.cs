using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;
using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IMainScreenWorkflow _mainScreenWorkflow;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IReportFileSaveService _reportFileSaveService;
    private MainUiState _state;
    private string _settingsStatusText;
    private int _historyMaxEntries;
    private bool _showVerificationHints;
    private ReportFormatOption _selectedReportFormat;
    private string _reportExportStatusText;
    private string _reportFileNameBase;
    private string _reportPlainTextContent;
    private string _reportMarkdownContent;
    private IReadOnlyCollection<RecentHistoryPresentation> _recentHistory;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public MainViewModel(
        IMainScreenWorkflow mainScreenWorkflow,
        ISettingsRepository settingsRepository,
        IReportFileSaveService reportFileSaveService)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _settingsRepository = settingsRepository;
        _reportFileSaveService = reportFileSaveService;
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusReady, UiStrings.ScanAction);
        _settingsStatusText = UiStrings.SettingsLoadError;
        _reportExportStatusText = UiStrings.ReportExportStatusNoData;
        _historyMaxEntries = AppSettings.Default.History.MaxEntries;
        _showVerificationHints = AppSettings.Default.WorkflowGuidance.ShowPostInstallVerificationHints;
        _selectedReportFormat = ReportFormatItems[0];
        _reportFileNameBase = "driverguardian-report";
        _reportPlainTextContent = string.Empty;
        _reportMarkdownContent = string.Empty;
        _recentHistory = Array.Empty<RecentHistoryPresentation>();
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportReportCommand { get; }
    public IReadOnlyList<ReportFormatOption> AvailableReportFormats => ReportFormatItems;

    public MainUiState State
    {
        get => _state;
        private set
        {
            _state = value;
            OnPropertyChanged();
        }
    }

    public int HistoryMaxEntries
    {
        get => _historyMaxEntries;
        set
        {
            var normalized = Math.Clamp(value, 10, 500);
            if (_historyMaxEntries == normalized)
            {
                return;
            }

            _historyMaxEntries = normalized;
            OnPropertyChanged();
        }
    }

    public bool ShowVerificationHints
    {
        get => _showVerificationHints;
        set
        {
            if (_showVerificationHints == value)
            {
                return;
            }

            _showVerificationHints = value;
            OnPropertyChanged();
        }
    }

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

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        private set
        {
            _settingsStatusText = value;
            OnPropertyChanged();
        }
    }


    public IReadOnlyCollection<RecentHistoryPresentation> RecentHistory
    {
        get => _recentHistory;
        private set
        {
            _recentHistory = value;
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

    private async Task ScanAsync()
    {
        State = State with { StatusText = UiStrings.StatusScanning };

        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);

        State = State with
        {
            StatusText = UiStrings.StatusReady,
            Results = ScanResultsPresentation.FromResult(result)
        };
        RecentHistory = RecentHistoryPresentation.FromResults(result.RecentHistory);
        _reportFileNameBase = result.ReportExportPayload.FileNameBase;
        _reportPlainTextContent = result.ReportExportPayload.PlainTextContent;
        _reportMarkdownContent = result.ReportExportPayload.MarkdownContent;
        ReportExportStatusText = UiStrings.ReportExportStatusReady;
    }


    private async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadRecentHistoryAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == settings.Reports.DefaultFormat);
        SettingsStatusText = UiStrings.SettingsLoaded;
    }


    private async Task LoadRecentHistoryAsync()
    {
        var entries = await _mainScreenWorkflow.GetRecentHistoryAsync(take: 5, CancellationToken.None);
        RecentHistory = RecentHistoryPresentation.FromResults(entries);
    }

    private async Task SaveSettingsAsync()
    {
        var current = await _settingsRepository.GetAsync(CancellationToken.None);
        var updated = current with
        {
            History = current.History with { MaxEntries = HistoryMaxEntries },
            Reports = current.Reports with { DefaultFormat = SelectedReportFormat.Value },
            WorkflowGuidance = current.WorkflowGuidance with { ShowPostInstallVerificationHints = ShowVerificationHints }
        };

        await _settingsRepository.SaveAsync(updated, CancellationToken.None);
        SettingsStatusText = UiStrings.SettingsSaved;
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

        var saved = _reportFileSaveService.TrySave(_reportFileNameBase, extension, filter, content);
        ReportExportStatusText = saved ? UiStrings.ReportExportStatusSaved : UiStrings.ReportExportStatusCanceled;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record ReportFormatOption(ShareableReportFormat Value, string DisplayName);
