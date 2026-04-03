using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class SettingsSectionViewModel : INotifyPropertyChanged
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IDiagnosticLogsFolderService _diagnosticLogsFolderService;
    private int _historyMaxEntries;
    private bool _showVerificationHints;
    private bool _isDiagnosticLoggingEnabled;
    private string _customDiagnosticLogFolderPath;
    private string _effectiveDiagnosticLogFolderPath;
    private string _settingsStatusText;
    private readonly Func<ShareableReportFormat> _selectedReportFormatProvider;

    public SettingsSectionViewModel(
        ISettingsRepository settingsRepository,
        IDiagnosticLogsFolderService diagnosticLogsFolderService,
        Func<ShareableReportFormat> selectedReportFormatProvider)
    {
        _settingsRepository = settingsRepository;
        _diagnosticLogsFolderService = diagnosticLogsFolderService;
        _selectedReportFormatProvider = selectedReportFormatProvider;
        _historyMaxEntries = AppSettings.Default.History.MaxEntries;
        _showVerificationHints = AppSettings.Default.WorkflowGuidance.ShowPostInstallVerificationHints;
        _isDiagnosticLoggingEnabled = AppSettings.Default.DiagnosticLogging.Enabled;
        _customDiagnosticLogFolderPath = string.Empty;
        _effectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(AppSettings.Default.DiagnosticLogging.CustomLogsFolderPath);
        _settingsStatusText = UiStrings.SettingsLoadError;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        OpenDiagnosticLogsFolderCommand = new AsyncRelayCommand(OpenDiagnosticLogsFolderAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveSettingsCommand { get; }

    public ICommand OpenDiagnosticLogsFolderCommand { get; }

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

    public async Task LoadSettingsAsync(Action<ShareableReportFormat> reportFormatConsumer)
    {
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        reportFormatConsumer(settings.Reports.DefaultFormat);
        IsDiagnosticLoggingEnabled = settings.DiagnosticLogging.Enabled;
        CustomDiagnosticLogFolderPath = settings.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        EffectiveDiagnosticLogFolderPath = _diagnosticLogsFolderService.ResolveEffectiveFolderPath(settings.DiagnosticLogging.CustomLogsFolderPath);
        SettingsStatusText = UiStrings.SettingsLoaded;
    }

    public void ApplyStatusMessage(string statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return;
        }

        SettingsStatusText = statusMessage;
    }

    private async Task SaveSettingsAsync()
    {
        var current = await _settingsRepository.GetAsync(CancellationToken.None);
        var updated = current with
        {
            History = current.History with { MaxEntries = HistoryMaxEntries },
            Reports = current.Reports with { DefaultFormat = _selectedReportFormatProvider() },
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
