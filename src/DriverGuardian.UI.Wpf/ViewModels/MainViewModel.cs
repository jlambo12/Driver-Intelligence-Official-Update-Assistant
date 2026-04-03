using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels.Sections;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IMainScreenWorkflow _mainScreenWorkflow;
    private readonly PreviewScenarioMainScreenWorkflow? _previewWorkflow;
    private readonly IDiagnosticLogsFolderService _diagnosticLogsFolderService;
    private readonly IOfficialSourceLauncher _officialSourceLauncher;
    private MainUiState _state;
    private PreviewScenarioOption? _selectedPreviewScenario;
    private string? _lastApprovedOfficialSourceUrl;

    public MainViewModel(
        IMainScreenWorkflow mainScreenWorkflow,
        ISettingsRepository settingsRepository,
        IReportFileSaveService reportFileSaveService,
        IDiagnosticLogsFolderService diagnosticLogsFolderService,
        IOfficialSourceLauncher officialSourceLauncher)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _previewWorkflow = mainScreenWorkflow as PreviewScenarioMainScreenWorkflow;
        _diagnosticLogsFolderService = diagnosticLogsFolderService;
        _officialSourceLauncher = officialSourceLauncher;
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusInitial, UiStrings.ScanAction);

        WorkflowSection = new WorkflowSectionViewModel(_state);
        HistorySection = new HistorySectionViewModel();
        ReportSection = new ReportSectionViewModel(reportFileSaveService);
        SettingsSection = new SettingsSectionViewModel(
            settingsRepository,
            diagnosticLogsFolderService.ResolveEffectiveFolderPath(AppSettings.Default.DiagnosticLogging.CustomLogsFolderPath));

        SettingsSection.PropertyChanged += OnSettingsSectionPropertyChanged;

        AvailablePreviewScenarios = BuildPreviewOptions();
        _selectedPreviewScenario = AvailablePreviewScenarios.FirstOrDefault();

        ScanCommand = new AsyncRelayCommand(ScanAsync);
        ApplyPreviewScenarioCommand = new AsyncRelayCommand(ApplyPreviewScenarioAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        OpenDiagnosticLogsFolderCommand = new AsyncRelayCommand(OpenDiagnosticLogsFolderAsync);
        OpenOfficialSourceCommand = new AsyncRelayCommand(OpenOfficialSourceAsync, () => CanOpenOfficialSource);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand ApplyPreviewScenarioCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand OpenDiagnosticLogsFolderCommand { get; }
    public ICommand OpenOfficialSourceCommand { get; }

    public bool CanOpenOfficialSource => TryGetApprovedOfficialSourceUri(out _);

    public string OpenOfficialSourceBlockReason => CanOpenOfficialSource
        ? string.Empty
        : State.Results.HasScanData
            ? State.Results.OfficialSourceSummary
            : "Запустите анализ, чтобы получить подтверждённый официальный источник.";

    public bool IsPreviewMode => _previewWorkflow is not null;
    public string PreviewModeBannerText => UiStrings.PreviewModeBanner;
    public IReadOnlyList<PreviewScenarioOption> AvailablePreviewScenarios { get; }

    public WorkflowSectionViewModel WorkflowSection { get; }
    public HistorySectionViewModel HistorySection { get; }
    public ReportSectionViewModel ReportSection { get; }
    public SettingsSectionViewModel SettingsSection { get; }

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

    public MainUiState State
    {
        get => _state;
        private set
        {
            _state = value;
            WorkflowSection.State = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        await SettingsSection.LoadSettingsAsync(_diagnosticLogsFolderService.ResolveEffectiveFolderPath(null));
        SyncEffectiveDiagnosticFolder();

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

    public void ApplyStartupRecoveryStatus(string recoveryMessage)
    {
        if (string.IsNullOrWhiteSpace(recoveryMessage))
        {
            return;
        }

        SettingsSection.ApplyStatusMessage(recoveryMessage);
        State = State with { StatusText = recoveryMessage };
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

    private async Task ExportReportAsync()
    {
        await ReportSection.ExportAsync(SettingsSection.SelectedReportFormat.Value);
    }

    private async Task OpenDiagnosticLogsFolderAsync()
    {
        await Task.Yield();

        var opened = _diagnosticLogsFolderService.OpenFolder(SettingsSection.EffectiveDiagnosticLogFolderPath);
        if (!opened)
        {
            SettingsSection.ApplyStatusMessage(UiStrings.SettingsLogsFolderOpenFailed);
        }
    }

    private async Task OpenOfficialSourceAsync()
    {
        await Task.Yield();

        if (!TryGetApprovedOfficialSourceUri(out var approvedUri) || approvedUri is null)
        {
            State = State with { StatusText = OpenOfficialSourceBlockReason };
            return;
        }

        if (!_officialSourceLauncher.Open(approvedUri))
        {
            State = State with { StatusText = "Не удалось открыть официальный источник. Проверьте браузер по умолчанию." };
            return;
        }

        State = State with { StatusText = $"Открыт официальный источник: {approvedUri.Host}" };
    }

    private bool TryGetApprovedOfficialSourceUri(out Uri? uri)
    {
        uri = null;

        if (!State.Results.HasScanData || string.IsNullOrWhiteSpace(_lastApprovedOfficialSourceUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(_lastApprovedOfficialSourceUrl, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = parsed;
        return true;
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
            HistorySection.RecentHistory = Array.Empty<RecentHistoryPresentation>();
            ReportSection.ClearPayload("driverguardian-preview-first-run");
            WorkflowSection.ShowSecondaryRecommendations = false;
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
        WorkflowSection.ShowSecondaryRecommendations = false;
        HistorySection.RecentHistory = RecentHistoryPresentation.FromResults(result.RecentHistory);
        _lastApprovedOfficialSourceUrl = result.OfficialSourceAction.ApprovedOfficialSourceUrl;
        ReportSection.ApplyWorkflowPayload(result.ReportExportPayload);
        OnPropertyChanged(nameof(CanOpenOfficialSource));
        OnPropertyChanged(nameof(OpenOfficialSourceBlockReason));

        if (OpenOfficialSourceCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private void OnSettingsSectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsSectionViewModel.CustomDiagnosticLogFolderPath))
        {
            SyncEffectiveDiagnosticFolder();
        }
    }

    private void SyncEffectiveDiagnosticFolder()
    {
        var effective = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(SettingsSection.CustomDiagnosticLogFolderPath);
        SettingsSection.ApplyEffectiveDiagnosticFolder(effective);
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
