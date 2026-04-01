using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.State;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IRecommendationPipeline _recommendationPipeline;
    private readonly IUserBoundaryErrorHandler _errorHandler;

    private ScanUiState _state = ScanUiState.Idle;
    private string _resultsPlaceholder;

    public MainViewModel(
        IScanOrchestrator scanOrchestrator,
        IRecommendationPipeline recommendationPipeline,
        IUserBoundaryErrorHandler errorHandler,
        LocalizedStrings localized)
    {
        _scanOrchestrator = scanOrchestrator;
        _recommendationPipeline = recommendationPipeline;
        _errorHandler = errorHandler;
        Localized = localized;
        _resultsPlaceholder = Localized.ResultsPlaceholder;

        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => State != ScanUiState.Scanning);
    }

    public LocalizedStrings Localized { get; }

    public ICommand StartScanCommand { get; }

    public ScanUiState State
    {
        get => _state;
        private set
        {
            _state = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText => State switch
    {
        ScanUiState.Idle => Localized.StatusIdle,
        ScanUiState.Scanning => Localized.StatusScanning,
        ScanUiState.Completed => Localized.StatusCompleted,
        _ => Localized.StatusError
    };

    public string ResultsPlaceholder
    {
        get => _resultsPlaceholder;
        private set
        {
            _resultsPlaceholder = value;
            RaisePropertyChanged();
        }
    }

    private async Task StartScanAsync()
    {
        try
        {
            State = ScanUiState.Scanning;
            var session = await _scanOrchestrator.RunScanAsync(CancellationToken.None);
            var summary = await _recommendationPipeline.BuildAsync(session, CancellationToken.None);

            ResultsPlaceholder = $"{Localized.ResultsHeader}: {summary.PotentiallyOutdatedCount}/{summary.TotalDevices}";
            State = ScanUiState.Completed;
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleAsync(ex, nameof(MainViewModel), CancellationToken.None);
            State = ScanUiState.Error;
            ResultsPlaceholder = Localized.StatusError;
        }
    }
}
