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
        _state = MainUiState.Initial(UiStrings.MainWindowTitle, UiStrings.StatusReady, UiStrings.ScanAction, UiStrings.VerificationReturnRunAction);
        ScanCommand = new AsyncRelayCommand(() => RunScanAsync(false));
        RunVerificationReturnCommand = new AsyncRelayCommand(() => RunScanAsync(true));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }

    public ICommand RunVerificationReturnCommand { get; }

    public MainUiState State
    {
        get => _state;
        private set
        {
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsManualInstallConfirmed));
        }
    }

    public bool IsManualInstallConfirmed
    {
        get => State.IsManualInstallConfirmed;
        set
        {
            if (State.IsManualInstallConfirmed == value)
            {
                return;
            }

            State = State with { IsManualInstallConfirmed = value };
        }
    }

    private async Task RunScanAsync(bool verificationReturn)
    {
        State = State with { StatusText = verificationReturn ? UiStrings.StatusVerifyingReturn : UiStrings.StatusScanning };

        var result = await _mainScreenWorkflow.RunScanAsync(CancellationToken.None);

        State = State with
        {
            StatusText = UiStrings.StatusReady,
            Results = ScanResultsPresentation.FromResult(result, State.IsManualInstallConfirmed)
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
