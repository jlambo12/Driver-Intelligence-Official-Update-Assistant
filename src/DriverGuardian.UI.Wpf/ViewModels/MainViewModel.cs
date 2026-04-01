using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IMainScreenWorkflow _mainScreenWorkflow;
    private MainUiState _state;
    private string? _latestShareableReportText;

    public MainViewModel(IMainScreenWorkflow mainScreenWorkflow)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _state = MainUiState.Initial(
            UiStrings.MainWindowTitle,
            UiStrings.StatusReady,
            UiStrings.ScanAction,
            UiStrings.ReportExportAction,
            UiStrings.ReportSectionTitle,
            UiStrings.ReportPreviewPlaceholder);
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand ExportReportCommand { get; }

    public MainUiState State
    {
        get => _state;
        private set
        {
            _state = value;
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

        _latestShareableReportText = result.ShareableReportText;
    }

    private Task ExportReportAsync()
    {
        if (string.IsNullOrWhiteSpace(_latestShareableReportText))
        {
            State = State with { StatusText = UiStrings.ReportStatusRequiresScan };
            return Task.CompletedTask;
        }

        Clipboard.SetText(_latestShareableReportText);
        State = State with
        {
            StatusText = UiStrings.ReportStatusPrepared,
            ReportPreviewText = _latestShareableReportText
        };

        return Task.CompletedTask;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
