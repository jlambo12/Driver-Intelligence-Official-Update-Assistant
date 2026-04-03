using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Infrastructure.DiagnosticLogging;
using DriverGuardian.Bootstrap.Runtime;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels;
using WpfApplication = System.Windows.Application;
using System.IO;

namespace DriverGuardian.UI.Wpf;

public partial class App : WpfApplication
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

        var previewModeEnabled = IsPreviewModeEnabled(e.Args);
        var runtime = previewModeEnabled
            ? null
            : ProductionRuntimeFactory.Create(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        var startupLogger = runtime?.StartupLogger ?? new NoOpDiagnosticLogger();

        try
        {
            ISettingsRepository settingsRepository = runtime?.SettingsRepository ?? new InMemorySettingsRepository();
            IMainScreenWorkflow mainScreenWorkflow = runtime?.MainScreenWorkflow ?? new PreviewScenarioMainScreenWorkflow();
            IDiagnosticLogsFolderService logsFolderService = runtime?.DiagnosticLogsFolderService
                ?? new DiagnosticLogsFolderService(GetDefaultLogsDirectory());

            var vm = new MainViewModel(
                mainScreenWorkflow,
                settingsRepository,
                new ReportFileSaveService(),
                logsFolderService);

            await vm.InitializeAsync();

            var window = new MainWindow { DataContext = vm };
            window.Show();
            await startupLogger.LogInfoAsync("app.startup.completed", "Primary startup flow completed.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await startupLogger.LogErrorAsync("app.startup.failed", "Primary startup flow failed. Recovery mode will be activated.", ex, CancellationToken.None);
            var recoveryStatus = $"Startup recovery mode: {ex.GetType().Name} — {ex.Message}";
            MessageBox.Show(
                $"Не удалось запустить приложение в production-режиме. Активирован recovery-режим (preview).{Environment.NewLine}{Environment.NewLine}{recoveryStatus}",
                "DriverGuardian startup recovery",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            var fallbackVm = new MainViewModel(
                new PreviewScenarioMainScreenWorkflow(),
                new InMemorySettingsRepository(),
                new ReportFileSaveService(),
                new DiagnosticLogsFolderService(GetDefaultLogsDirectory()));
            await fallbackVm.InitializeAsync();
            fallbackVm.ApplyStartupRecoveryStatus(recoveryStatus);
            var fallbackWindow = new MainWindow { DataContext = fallbackVm };
            fallbackWindow.Show();
            await startupLogger.LogWarningAsync("app.startup.recovery_mode.enabled", "Application is running in recovery preview mode after startup failure.", CancellationToken.None);
        }
    }

    private static string GetDefaultLogsDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DriverGuardian",
            "Logs");

    private static bool IsPreviewModeEnabled(string[] args)
        => args.Any(arg => string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase));
}
