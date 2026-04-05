using DriverGuardian.Application.Abstractions;
using DriverGuardian.Infrastructure.DiagnosticLogging;

namespace DriverGuardian.UI.Wpf.Services;

public interface IAppStartupRuntime
{
    IMainScreenWorkflow MainScreenWorkflow { get; }
    ISettingsRepository SettingsRepository { get; }
    IDiagnosticLogsFolderService DiagnosticLogsFolderService { get; }
    IDiagnosticLogger StartupLogger { get; }
}
