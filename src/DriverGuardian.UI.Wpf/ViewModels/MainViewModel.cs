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
    private readonly IDiagnosticLogsFolderService _diagnosticLogsFolderService;
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
    private bool _isDiagnosticLoggingEnabled;
    private string _customDiagnosticLogFolderPath;
    private string _effectiveDiagnosticLogFolderPath;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public MainViewModel(
        IMainScreenWorkflow mainScreenWorkflow,
        ISettingsRepository settingsRepository,
        IReportFileSaveService reportFileSaveService,
        IDiagnosticLogsFolderService diagnosticLogsFolderService)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _settingsRepository = settingsRepository;
        _reportFileSaveService = reportFileSaveService;
        _diagnosticLogsFolderService = diagnosticLogsFolderService;
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
        AvailablePreviewScenarios = BuildPreviewOptions();
        _selectedPreviewScenario = AvailablePreviewScenarios.FirstOrDefault();
        _showSecondaryRecommendations = false;
        _isDiagnosticLoggingEnabled = AppSettings.Default.DiagnosticLogging.Enabled;
        _customDiagnosticLogFolderPath = string.Empty;
        _effectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(AppSettings.Default.DiagnosticLogging.CustomLogsFolderPath);
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        ApplyPreviewScenarioCommand = new AsyncRelayCommand(ApplyPreviewScenarioAsync);
        OpenDiagnosticLogsFolderCommand = new AsyncRelayCommand(OpenDiagnosticLogsFolderAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ApplyPreviewScenarioCommand { get; }
    public ICommand OpenDiagnosticLogsFolderCommand { get; }
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


    public bool IsDiagnosticLoggingEnabled
    {
        get => _isDiagnosticLoggingEnabled;
        set
        {
            if (_isDiagnosticLoggingEnabled == value)
            {
                return;
            }

            _isDiagnosticLoggingEnabled = value;
            OnPropertyChanged();
        }
    }

    public string CustomDiagnosticLogFolderPath
    {
        get => _customDiagnosticLogFolderPath;
        set
        {
            var nextValue = value ?? string.Empty;
            if (_customDiagnosticLogFolderPath == nextValue)
            {
                return;
            }

            _customDiagnosticLogFolderPath = nextValue;
            EffectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(nextValue);
            OnPropertyChanged();
        }
    }

    public string EffectiveDiagnosticLogFolderPath
    {
        get => _effectiveDiagnosticLogFolderPath;
        private set
        {
            if (_effectiveDiagnosticLogFolderPath == value)
            {
                return;
            }

            _effectiveDiagnosticLogFolderPath = value;
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

    public async Task InitializeAsync()
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

        State = State with { StatusText = UiStrings.StatusScanning };

        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);
        ApplyWorkflowResult(result, isPreview: false);
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
        State = MainUiStateFactory.CreateFromWorkflowResult(result, isPreview, scenarioName);
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
        IsDiagnosticLoggingEnabled = settings.DiagnosticLogging.Enabled;
        CustomDiagnosticLogFolderPath = settings.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        EffectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(settings.DiagnosticLogging.CustomLogsFolderPath);
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
                Enabled = IsDiagnosticLoggingEnabled,
                CustomLogsFolderPath = string.IsNullOrWhiteSpace(CustomDiagnosticLogFolderPath)
                    ? null
                    : CustomDiagnosticLogFolderPath.Trim()
            }
        };

        var normalized = updated.Normalize();
        await _settingsRepository.SaveAsync(normalized, CancellationToken.None);
        CustomDiagnosticLogFolderPath = normalized.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        EffectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(normalized.DiagnosticLogging.CustomLogsFolderPath);
        SettingsStatusText = UiStrings.SettingsSaved;
    }

    private async Task OpenDiagnosticLogsFolderAsync()
    {
        await Task.Yield();

        var opened = _diagnosticLogsFolderService.OpenFolder(EffectiveDiagnosticLogFolderPath);
        if (!opened)
        {
            SettingsStatusText = UiStrings.SettingsLogsFolderOpenFailed;
        }
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
