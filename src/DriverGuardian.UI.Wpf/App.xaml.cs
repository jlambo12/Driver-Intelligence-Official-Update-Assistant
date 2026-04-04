using System;
using System.Globalization;
using System.Windows;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.ViewModels;
using WpfApplication = System.Windows.Application;

namespace DriverGuardian.UI.Wpf;

public partial class App : WpfApplication
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

        using var startupCts = new CancellationTokenSource();
        var orchestrator = new AppStartupOrchestrator(
            new LocalAppDataStartupRuntimeProvider(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
            runtime => new MainViewModel(
                runtime.MainScreenWorkflow,
                runtime.SettingsRepository,
                new ReportFileSaveService(),
                runtime.DiagnosticLogsFolderService,
                new OfficialSourceLauncher()));

        try
        {
            var startup = await orchestrator.StartAsync(startupCts.Token);

            var window = new MainWindow { DataContext = startup.ViewModel };
            window.Show();
            await startup.Runtime.StartupLogger.LogInfoAsync("app.startup.completed", "Primary startup flow completed.", CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            Shutdown(-1);
        }
        catch (Exception ex)
        {
            // startup logger may be unavailable if creation failed before runtime was built
            MessageBox.Show(
                string.Format(UiStrings.StartupErrorMessageFormat, ex.GetType().Name, ex.Message),
                UiStrings.StartupErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
