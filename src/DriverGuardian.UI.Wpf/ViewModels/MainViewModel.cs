using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IMainScreenWorkflow _mainScreenWorkflow;
    private readonly ISettingsRepository _settingsRepository;
    private MainUiState _state;
    private string _settingsStatusText;
    private int _historyMaxEntries;
    private bool _showVerificationHints;
    private ReportFormatOption _selectedReportFormat;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public MainViewModel(IMainScreenWorkflow mainScreenWorkflow, ISettingsRepository settingsRepository)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _settingsRepository = settingsRepository;
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusReady, UiStrings.ScanAction);
        _settingsStatusText = UiStrings.SettingsLoadError;
        _historyMaxEntries = AppSettings.Default.History.MaxEntries;
        _showVerificationHints = AppSettings.Default.WorkflowGuidance.ShowPostInstallVerificationHints;
        _selectedReportFormat = ReportFormatItems[0];
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        VerifyReturnCommand = new AsyncRelayCommand(VerifyReturnAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        _ = LoadSettingsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand VerifyReturnCommand { get; }
    public ICommand SaveSettingsCommand { get; }
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

    private async Task ScanAsync()
    {
        State = State with { StatusText = UiStrings.StatusScanning };

        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);

        State = State with
        {
            StatusText = UiStrings.StatusReady,
            Results = ScanResultsPresentation.FromResult(result)
        };
    }

    private async Task VerifyReturnAsync()
    {
        State = State with { StatusText = UiStrings.StatusScanning };

        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);

        State = State with
        {
            StatusText = UiStrings.StatusReady,
            Results = ScanResultsPresentation.FromResult(result)
        };
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == settings.Reports.DefaultFormat);
        SettingsStatusText = UiStrings.SettingsLoaded;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record ReportFormatOption(ShareableReportFormat Value, string DisplayName);
