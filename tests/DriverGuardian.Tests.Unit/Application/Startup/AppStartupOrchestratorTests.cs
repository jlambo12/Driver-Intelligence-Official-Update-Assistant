using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Settings;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.Tests.Unit.Application.Startup;

public sealed class AppStartupOrchestratorTests
{
    [Fact]
    public async Task StartAsync_ShouldInitializeViewModelWithConfiguredScanProfile()
    {
        var repository = new InMemorySettingsRepository();
        await repository.SaveAsync(
            AppSettings.Default with { ScanCoverage = new ScanCoveragePreferences(DeviceScanProfile.Comprehensive) },
            CancellationToken.None);

        var orchestrator = new AppStartupOrchestrator(
            new FakeRuntimeProvider(new FakeStartupRuntime(repository)),
            runtime => new MainViewModel(
                runtime.MainScreenWorkflow,
                runtime.SettingsRepository,
                new FakeReportFileSaveService(),
                runtime.DiagnosticLogsFolderService,
                new FakeOfficialSourceLauncher()));

        var startup = await orchestrator.StartAsync(CancellationToken.None);

        Assert.Equal(DeviceScanProfile.Comprehensive, startup.ViewModel.SettingsSection.SelectedScanProfile.Value);
    }

    [Fact]
    public async Task StartAsync_ShouldRespectCancellationBeforeRuntimeCreation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var provider = new FakeRuntimeProvider(new FakeStartupRuntime(new InMemorySettingsRepository()));
        var orchestrator = new AppStartupOrchestrator(
            provider,
            runtime => new MainViewModel(
                runtime.MainScreenWorkflow,
                runtime.SettingsRepository,
                new FakeReportFileSaveService(),
                runtime.DiagnosticLogsFolderService,
                new FakeOfficialSourceLauncher()));

        await Assert.ThrowsAsync<OperationCanceledException>(() => orchestrator.StartAsync(cts.Token));
        Assert.False(provider.WasCalled);
    }

    private sealed class FakeRuntimeProvider(IAppStartupRuntime runtime) : IAppStartupRuntimeProvider
    {
        public bool WasCalled { get; private set; }

        public Task<IAppStartupRuntime> CreateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasCalled = true;
            return Task.FromResult(runtime);
        }
    }

    private sealed class FakeStartupRuntime(ISettingsRepository settingsRepository) : IAppStartupRuntime
    {
        public IMainScreenWorkflow MainScreenWorkflow { get; } = new StubMainScreenWorkflow();
        public ISettingsRepository SettingsRepository { get; } = settingsRepository;
        public IDiagnosticLogsFolderService DiagnosticLogsFolderService { get; } = new FakeDiagnosticLogsFolderService();
        public IDiagnosticLogger StartupLogger { get; } = new NoOpDiagnosticLogger();
    }

    private sealed class StubMainScreenWorkflow : IMainScreenWorkflow
    {
        public Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
            => Task.FromResult(new MainScreenWorkflowResult(
                ScanExecutionStatus.Completed,
                [],
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                "ru-RU",
                Guid.NewGuid(),
                new ReportExportPayload("none", string.Empty, string.Empty),
                [],
                new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, OfficialSourceActionTarget.SourcePage, string.Empty, null, null),
                []));
    }

    private sealed class FakeReportFileSaveService : IReportFileSaveService
    {
        public ReportFileSaveResult Save(string defaultFileName, string extension, string filter, string content)
            => ReportFileSaveResult.Saved;
    }

    private sealed class FakeDiagnosticLogsFolderService : IDiagnosticLogsFolderService
    {
        public string ResolveEffectiveFolderPath(string? customFolderPath)
            => string.IsNullOrWhiteSpace(customFolderPath) ? "C:/logs/default" : customFolderPath.Trim();

        public bool OpenFolder(string folderPath) => true;
    }

    private sealed class FakeOfficialSourceLauncher : IOfficialSourceLauncher
    {
        public bool Open(Uri uri) => true;
    }

    private sealed class NoOpDiagnosticLogger : IDiagnosticLogger
    {
        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
