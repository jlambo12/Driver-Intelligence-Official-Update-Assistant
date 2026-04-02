using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
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
    private readonly IDiagnosticLogger _diagnosticLogger;
    private readonly PreviewScenarioMainScreenWorkflow? _previewWorkflow;
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
    private PreviewScenarioOption? _selectedPreviewScenario;
    private bool _showSecondaryRecommendations;
    private bool _diagnosticLoggingEnabled;
    private string _customLogFolderPath;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public MainViewModel(
        IMainScreenWorkflow mainScreenWorkflow,
        ISettingsRepository settingsRepository,
        IReportFileSaveService reportFileSaveService,
        IDiagnosticLogger diagnosticLogger)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _settingsRepository = settingsRepository;
        _reportFileSaveService = reportFileSaveService;
        _diagnosticLogger = diagnosticLogger;
        _previewWorkflow = mainScreenWorkflow as PreviewScenarioMainScreenWorkflow;
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusInitial, UiStrings.ScanAction);
        _settingsStatusText = UiStrings.SettingsLoadError;
        _reportExportStatusText = UiStrings.ReportExportStatusNoData;
        _historyMaxEntries = AppSettings.Default.History.MaxEntries;
        _showVerificationHints = AppSettings.Default.WorkflowGuidance.ShowPostInstallVerificationHints;
        _selectedReportFormat = ReportFormatItems[0];
        _reportFileNameBase = "driverguardian-report";
        _reportPlainTextContent = string.Empty;
        _reportMarkdownContent = string.Empty;
        _recentHistory = Array.Empty<RecentHistoryPresentation>();
        _diagnosticLoggingEnabled = AppSettings.Default.DiagnosticLogging.Enabled;
        _customLogFolderPath = string.Empty;
        AvailablePreviewScenarios = BuildPreviewOptions();
        _selectedPreviewScenario = AvailablePreviewScenarios.FirstOrDefault();
        _showSecondaryRecommendations = false;
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        OpenLogsFolderCommand = new AsyncRelayCommand(OpenLogsFolderAsync);
        ApplyPreviewScenarioCommand = new AsyncRelayCommand(ApplyPreviewScenarioAsync);
        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand ApplyPreviewScenarioCommand { get; }
    public IReadOnlyList<ReportFormatOption> AvailableReportFormats => ReportFormatItems;

    public bool IsPreviewMode => _previewWorkflow is not null;

    public string PreviewModeBannerText => UiStrings.PreviewModeBanner;

    public IReadOnlyList<PreviewScenarioOption> AvailablePreviewScenarios { get; }

    public PreviewScenarioOption? SelectedPreviewScenario
    {
        get => _selectedPreviewScenario;
        set
        {
            if (_selectedPreviewScenario == value)
            {
                return;
            }

            _selectedPreviewScenario = value;
            OnPropertyChanged();
        }
    }


    public bool ShowSecondaryRecommendations
    {
        get => _showSecondaryRecommendations;
        set
        {
            if (_showSecondaryRecommendations == value)
            {
                return;
            }

            _showSecondaryRecommendations = value;
            OnPropertyChanged();
        }
    }

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

    public bool DiagnosticLoggingEnabled
    {
        get => _diagnosticLoggingEnabled;
        set
        {
            if (_diagnosticLoggingEnabled == value)
            {
                return;
            }

            _diagnosticLoggingEnabled = value;
            OnPropertyChanged();
        }
    }

    public string CustomLogFolderPath
    {
        get => _customLogFolderPath;
        set
        {
            if (_customLogFolderPath == value)
            {
                return;
            }

            _customLogFolderPath = value;
            OnPropertyChanged();
        }
    }

    public string EffectiveLogFolderPath => _diagnosticLogger.GetEffectiveLogDirectory();

    private async Task InitializeAsync()
    {
        await LoadSettingsAsync();

        if (!IsPreviewMode)
        {
            return;
        }

        State = State with
        {
            TitleText = UiStrings.PreviewWindowTitle,
            ScanButtonText = UiStrings.PreviewApplyScenarioAction
        };

        await ApplyPreviewScenarioAsync();
    }

    private async Task ScanAsync()
    {
        if (IsPreviewMode)
        {
            await ApplyPreviewScenarioAsync();
            return;
        }

        await _diagnosticLogger.LogInfoAsync("scan.user.start", "Пользователь запустил анализ из главного окна.", CancellationToken.None);
        State = State with { StatusText = UiStrings.StatusScanning };

        try
        {
            var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);
            ApplyWorkflowResult(result, isPreview: false);
        }
        catch (Exception ex)
        {
            await _diagnosticLogger.LogErrorAsync("scan.ui.failed", "Ошибка выполнения анализа на уровне ViewModel.", ex, CancellationToken.None);
            State = State with { StatusText = UiStrings.StatusScanFailed };
        }
    }

    private async Task ApplyPreviewScenarioAsync()
    {
        if (_previewWorkflow is null)
        {
            return;
        }

        var scenario = SelectedPreviewScenario ?? AvailablePreviewScenarios.First();
        _previewWorkflow.SelectScenario(scenario.Id);

        if (scenario.Id == PreviewScenarioId.FirstRunPreScan)
        {
            RecentHistory = Array.Empty<RecentHistoryPresentation>();
            _reportPlainTextContent = string.Empty;
            _reportMarkdownContent = string.Empty;
            _reportFileNameBase = "driverguardian-preview-first-run";
            ReportExportStatusText = UiStrings.ReportExportStatusNoData;
            ShowSecondaryRecommendations = false;
            State = MainUiState.Initial(
                UiStrings.PreviewWindowTitle,
                string.Format(UiStrings.PreviewModeStatusFormat, scenario.DisplayName),
                UiStrings.PreviewApplyScenarioAction);
            return;
        }

        var result = await _previewWorkflow.RunScanAsync(CancellationToken.None);
        ApplyWorkflowResult(result, isPreview: true, scenarioName: scenario.DisplayName);
    }

    private void ApplyWorkflowResult(MainScreenWorkflowResult result, bool isPreview, string? scenarioName = null)
    {
        var status = result.RecommendedCount > 0
            ? UiStrings.StatusScanCompletedReady
            : UiStrings.StatusScanCompletedNoAction;

        if (isPreview)
        {
            status = string.Format(UiStrings.PreviewModeStatusFormat, scenarioName ?? string.Empty);
        }

        State = State with
        {
            TitleText = isPreview ? UiStrings.PreviewWindowTitle : UiStrings.MainWindowTitle,
            ScanButtonText = isPreview ? UiStrings.PreviewApplyScenarioAction : UiStrings.ScanAction,
            StatusText = status,
            Results = ScanResultsPresentation.FromResult(result)
        };
        ShowSecondaryRecommendations = false;
        RecentHistory = RecentHistoryPresentation.FromResults(result.RecentHistory);
        _reportFileNameBase = result.ReportExportPayload.FileNameBase;
        _reportPlainTextContent = result.ReportExportPayload.PlainTextContent;
        _reportMarkdownContent = result.ReportExportPayload.MarkdownContent;
        ReportExportStatusText = string.IsNullOrWhiteSpace(_reportPlainTextContent)
            ? UiStrings.ReportExportStatusNoData
            : UiStrings.ReportExportStatusReady;
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == settings.Reports.DefaultFormat);
        DiagnosticLoggingEnabled = settings.DiagnosticLogging.Enabled;
        CustomLogFolderPath = settings.DiagnosticLogging.CustomFolderPath ?? string.Empty;
        OnPropertyChanged(nameof(EffectiveLogFolderPath));
        SettingsStatusText = UiStrings.SettingsLoaded;
    }

    private async Task SaveSettingsAsync()
    {
        var current = await _settingsRepository.GetAsync(CancellationToken.None);
        var updated = current with
        {
            History = current.History with { MaxEntries = HistoryMaxEntries },
            Reports = current.Reports with { DefaultFormat = SelectedReportFormat.Value },
            WorkflowGuidance = current.WorkflowGuidance with { ShowPostInstallVerificationHints = ShowVerificationHints },
            DiagnosticLogging = current.DiagnosticLogging with
            {
                Enabled = DiagnosticLoggingEnabled,
                CustomFolderPath = string.IsNullOrWhiteSpace(CustomLogFolderPath) ? null : CustomLogFolderPath.Trim()
            }
        };

        await _settingsRepository.SaveAsync(updated, CancellationToken.None);
        SettingsStatusText = UiStrings.SettingsSaved;
        OnPropertyChanged(nameof(EffectiveLogFolderPath));
    }

    private async Task OpenLogsFolderAsync()
    {
        await Task.Yield();
        SettingsStatusText = _diagnosticLogger.TryOpenEffectiveLogDirectory()
            ? UiStrings.SettingsLogsOpenSuccess
            : UiStrings.SettingsLogsOpenFailed;
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

    private IReadOnlyList<PreviewScenarioOption> BuildPreviewOptions()
    {
        if (_previewWorkflow is null)
        {
            return Array.Empty<PreviewScenarioOption>();
        }

        return _previewWorkflow.AvailableScenarios
            .Select(id => new PreviewScenarioOption(id, GetScenarioName(id)))
            .ToArray();
    }

    private static string GetScenarioName(PreviewScenarioId scenarioId)
        => scenarioId switch
        {
            PreviewScenarioId.FirstRunPreScan => UiStrings.PreviewScenarioFirstRun,
            PreviewScenarioId.NoActionableRecommendation => UiStrings.PreviewScenarioNoAction,
            PreviewScenarioId.RecommendationWithLimitedEvidence => UiStrings.PreviewScenarioLimitedEvidence,
            PreviewScenarioId.RecommendationReadyForManualAction => UiStrings.PreviewScenarioManualReady,
            PreviewScenarioId.VerificationReturnGuidance => UiStrings.PreviewScenarioVerificationReturn,
            PreviewScenarioId.PopulatedHistoryAndExport => UiStrings.PreviewScenarioHistoryExport,
            _ => UiStrings.PreviewScenarioFirstRun
        };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record ReportFormatOption(ShareableReportFormat Value, string DisplayName);
public sealed record PreviewScenarioOption(PreviewScenarioId Id, string DisplayName);
