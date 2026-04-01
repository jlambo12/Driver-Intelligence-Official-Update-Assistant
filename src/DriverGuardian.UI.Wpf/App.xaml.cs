using System.Globalization;
using System.Windows;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.History;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Application.Recommendations;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Scanning;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Infrastructure.Audit;
using DriverGuardian.Infrastructure.History;
using DriverGuardian.Infrastructure.Settings;
using DriverGuardian.Infrastructure.Time;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;
using DriverGuardian.ProviderAdapters.Official.Registry;
using DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;
using DriverGuardian.SystemAdapters.Windows.DriverInspection;
using DriverGuardian.UI.Wpf.Services;
using DriverGuardian.UI.Wpf.ViewModels;

namespace DriverGuardian.UI.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

        ISettingsRepository settingsRepository = new InMemorySettingsRepository();
        IMainScreenWorkflow mainScreenWorkflow = IsPreviewModeEnabled(e.Args)
            ? new PreviewScenarioMainScreenWorkflow()
            : BuildProductionWorkflow(settingsRepository);

        var vm = new MainViewModel(mainScreenWorkflow, settingsRepository, new ReportFileSaveService());
        var window = new MainWindow { DataContext = vm };
        window.Show();
    }

    private static bool IsPreviewModeEnabled(string[] args)
        => args.Any(arg => string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase));

    private static IMainScreenWorkflow BuildProductionWorkflow(ISettingsRepository settingsRepository)
    {
        IClock clock = new SystemClock();
        IDeviceDiscoveryService discovery = new WindowsDeviceDiscoveryService();
        IDriverMetadataInspector inspector = new WindowsDriverMetadataInspectorStub();
        IDriverInspectionOrchestrator inspectionOrchestrator = new DriverInspectionOrchestrator(inspector);
        IScanOrchestrator scanOrchestrator = new ScanOrchestrator(discovery, inspectionOrchestrator, clock);
        IRecommendationPipeline recommendationPipeline = new RecommendationPipeline();
        IProviderRegistry providerRegistry = new OfficialProviderRegistryStub();
        IProviderCatalogSummaryService providerSummaryService = new ProviderCatalogSummaryService(providerRegistry);
        IAuditWriter auditWriter = new InMemoryAuditWriter();
        IResultHistoryRepository resultHistoryRepository = new InMemoryResultHistoryRepository();
        var openOfficialSourceActionEvaluator = new OpenOfficialSourceActionEvaluator();
        IShareableReportBuilder reportBuilder = new ShareableReportBuilder();

        return new MainScreenWorkflow(
            scanOrchestrator,
            recommendationPipeline,
            providerSummaryService,
            settingsRepository,
            auditWriter,
            resultHistoryRepository,
            openOfficialSourceActionEvaluator,
            reportBuilder);
    }
}
