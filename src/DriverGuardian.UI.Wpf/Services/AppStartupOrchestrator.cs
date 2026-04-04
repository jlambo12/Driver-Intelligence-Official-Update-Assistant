using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf.Services;

public sealed class AppStartupOrchestrator(
    IAppStartupRuntimeProvider runtimeProvider,
    Func<IAppStartupRuntime, MainViewModel> viewModelFactory)
{
    public async Task<AppStartupExecutionResult> StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runtime = await runtimeProvider.CreateAsync(cancellationToken);
        var viewModel = viewModelFactory(runtime);
        await viewModel.InitializeAsync(cancellationToken);

        return new AppStartupExecutionResult(runtime, viewModel);
    }
}

public sealed record AppStartupExecutionResult(IAppStartupRuntime Runtime, MainViewModel ViewModel);
