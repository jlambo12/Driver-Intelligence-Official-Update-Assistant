using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public MainViewModel(IMainScreenWorkflow mainScreenWorkflow)
    {
        _mainScreenWorkflow = mainScreenWorkflow;
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusReady, UiStrings.ScanAction);
        ScanCommand = new AsyncRelayCommand(ScanAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }

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
            LastScanSummary = string.Format(
                UiStrings.LastScanSummaryFormat,
                result.DriverCount,
                result.RecommendationCount,
                result.ProviderCount,
                result.UiCulture)
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
