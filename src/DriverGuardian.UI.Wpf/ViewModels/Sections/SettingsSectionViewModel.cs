using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class SettingsSectionViewModel : INotifyPropertyChanged
{
    private readonly ISettingsRepository _settingsRepository;
    private int _historyMaxEntries;
    private bool _showVerificationHints;
    private bool _isDiagnosticLoggingEnabled;
    private string _customDiagnosticLogFolderPath;
    private string _effectiveDiagnosticLogFolderPath;
    private string _settingsStatusText;
    private readonly string _defaultDiagnosticLogFolderPath;
    private ReportFormatOption _selectedReportFormat;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    public SettingsSectionViewModel(
        ISettingsRepository settingsRepository,
        string defaultDiagnosticLogFolderPath)
    {
        _settingsRepository = settingsRepository;
        _defaultDiagnosticLogFolderPath = defaultDiagnosticLogFolderPath;
        _historyMaxEntries = AppSettings.Default.History.MaxEntries;
        _showVerificationHints = AppSettings.Default.WorkflowGuidance.ShowPostInstallVerificationHints;
        _isDiagnosticLoggingEnabled = AppSettings.Default.DiagnosticLogging.Enabled;
        _customDiagnosticLogFolderPath = string.Empty;
        _effectiveDiagnosticLogFolderPath = defaultDiagnosticLogFolderPath;
        _settingsStatusText = UiStrings.SettingsLoadError;
        _selectedReportFormat = ReportFormatItems[0];

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveSettingsCommand { get; }

    public IReadOnlyList<ReportFormatOption> AvailableReportFormats => ReportFormatItems;

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
            EffectiveDiagnosticLogFolderPath = string.IsNullOrWhiteSpace(nextValue)
                ? _defaultDiagnosticLogFolderPath
                : nextValue.Trim();
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

    public async Task LoadSettingsAsync(string defaultDiagnosticLogFolderPath)
    {
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);
        HistoryMaxEntries = settings.History.MaxEntries;
        ShowVerificationHints = settings.WorkflowGuidance.ShowPostInstallVerificationHints;
        SelectedReportFormat = ReportFormatItems.First(option => option.Value == settings.Reports.DefaultFormat);
        IsDiagnosticLoggingEnabled = settings.DiagnosticLogging.Enabled;
        CustomDiagnosticLogFolderPath = settings.DiagnosticLogging.CustomLogsFolderPath ?? string.Empty;
        EffectiveDiagnosticLogFolderPath = string.IsNullOrWhiteSpace(settings.DiagnosticLogging.CustomLogsFolderPath)
            ? defaultDiagnosticLogFolderPath
            : settings.DiagnosticLogging.CustomLogsFolderPath.Trim();
        SettingsStatusText = UiStrings.SettingsLoaded;
    }

    public void ApplyStatusMessage(string statusMessage)
    {
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            SettingsStatusText = statusMessage;
        }
    }

    public void ApplyEffectiveDiagnosticFolder(string folderPath)
    {
        EffectiveDiagnosticLogFolderPath = folderPath;
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
        SettingsStatusText = UiStrings.SettingsSaved;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
