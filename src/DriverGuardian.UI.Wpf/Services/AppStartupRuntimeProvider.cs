using DriverGuardian.Bootstrap.Runtime;

namespace DriverGuardian.UI.Wpf.Services;

public interface IAppStartupRuntimeProvider
{
    Task<IAppStartupRuntime> CreateAsync(CancellationToken cancellationToken);
}

public sealed class LocalAppDataStartupRuntimeProvider(string localAppDataRoot) : IAppStartupRuntimeProvider
{
    public async Task<IAppStartupRuntime> CreateAsync(CancellationToken cancellationToken)
    {
        var runtime = await ProductionRuntimeFactory.CreateAsync(localAppDataRoot, cancellationToken);
        return new AppStartupRuntimeAdapter(runtime);
    }

    private sealed class AppStartupRuntimeAdapter(ProductionRuntime runtime) : IAppStartupRuntime
    {
        public IMainScreenWorkflow MainScreenWorkflow { get; } = runtime.MainScreenWorkflow;
        public ISettingsRepository SettingsRepository { get; } = runtime.SettingsRepository;
        public IDiagnosticLogsFolderService DiagnosticLogsFolderService { get; } = runtime.DiagnosticLogsFolderService;
        public IDiagnosticLogger StartupLogger { get; } = runtime.StartupLogger;
    }
}
