using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed partial class SettingsSectionViewModel : INotifyPropertyChanged
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
    private ScanProfileOption _selectedScanProfile;

    private static readonly IReadOnlyList<ReportFormatOption> ReportFormatItems =
    [
        new(ShareableReportFormat.Markdown, UiStrings.SettingsReportFormatMarkdown),
        new(ShareableReportFormat.PlainText, UiStrings.SettingsReportFormatPlainText)
    ];

    private static readonly IReadOnlyList<ScanProfileOption> ScanProfileItems =
    [
        new(DeviceScanProfile.Balanced, UiStrings.SettingsScanProfileBalanced),
        new(DeviceScanProfile.Minimal, UiStrings.SettingsScanProfileMinimal),
        new(DeviceScanProfile.Comprehensive, UiStrings.SettingsScanProfileComprehensive)
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
        _selectedScanProfile = ScanProfileItems[0];

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SaveSettingsCommand { get; }

    public IReadOnlyList<ReportFormatOption> AvailableReportFormats => ReportFormatItems;
    public IReadOnlyList<ScanProfileOption> AvailableScanProfiles => ScanProfileItems;

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

    public ScanProfileOption SelectedScanProfile
    {
        get => _selectedScanProfile;
        set
        {
            if (_selectedScanProfile.Equals(value))
            {
                return;
            }

            _selectedScanProfile = value;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
