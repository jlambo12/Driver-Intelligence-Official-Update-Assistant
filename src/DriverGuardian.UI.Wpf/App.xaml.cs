using System;
using System.Globalization;
using System.Windows;
using DriverGuardian.Bootstrap.Runtime;
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

        var runtime = ProductionRuntimeFactory.Create(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        var startupLogger = runtime.StartupLogger;

        try
        {
            var vm = new MainViewModel(
                runtime.MainScreenWorkflow,
                runtime.SettingsRepository,
                new ReportFileSaveService(),
                runtime.DiagnosticLogsFolderService,
                new OfficialSourceLauncher());

            await vm.InitializeAsync();

            var window = new MainWindow { DataContext = vm };
            window.Show();
            await startupLogger.LogInfoAsync("app.startup.completed", "Primary startup flow completed.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await startupLogger.LogErrorAsync("app.startup.failed", "Primary startup flow failed.", ex, CancellationToken.None);
            MessageBox.Show(
                string.Format(UiStrings.StartupErrorMessageFormat, ex.GetType().Name, ex.Message),
                UiStrings.StartupErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
