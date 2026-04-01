using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Commands;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;

namespace DriverGuardian.UI.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IRecommendationPipeline _recommendationPipeline;
    private readonly IProviderRegistry _providerRegistry;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IAuditWriter _auditWriter;
    private MainUiState _state;

    public MainViewModel(
        IScanOrchestrator scanOrchestrator,
        IRecommendationPipeline recommendationPipeline,
        IProviderRegistry providerRegistry,
        ISettingsRepository settingsRepository,
        IAuditWriter auditWriter)
    {
        _scanOrchestrator = scanOrchestrator;
        _recommendationPipeline = recommendationPipeline;
        _providerRegistry = providerRegistry;
        _settingsRepository = settingsRepository;
        _auditWriter = auditWriter;

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

        var result = await _scanOrchestrator.RunAsync(CancellationToken.None);
        var recommendations = await _recommendationPipeline.BuildAsync(result.Drivers, CancellationToken.None);
        var providers = _providerRegistry.GetProviders();
        var settings = await _settingsRepository.GetAsync(CancellationToken.None);

        await _auditWriter.WriteAsync($"scan:{result.Session.Id}", CancellationToken.None);

        State = State with
        {
            StatusText = UiStrings.StatusReady,
            LastScanSummary = string.Format(
                UiStrings.LastScanSummaryFormat,
                result.Drivers.Count,
                recommendations.Count(r => r.HasRecommendation),
                providers.Count,
                settings.UiCulture)
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
