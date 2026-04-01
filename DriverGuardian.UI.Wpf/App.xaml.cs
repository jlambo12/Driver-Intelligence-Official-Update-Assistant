using DriverGuardian.Application.DependencyInjection;
using DriverGuardian.Infrastructure.DependencyInjection;
using DriverGuardian.ProviderAdapters.Official.DependencyInjection;
using DriverGuardian.SystemAdapters.Windows.DependencyInjection;
using DriverGuardian.UI.Wpf.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DriverGuardian.UI.Wpf;

public partial class App : System.Windows.Application
{
    public IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddApplicationCore()
            .AddInfrastructureFoundation()
            .AddWindowsSystemAdapters()
            .AddOfficialProviderAdapters()
            .AddUiFoundation();

        Services = serviceCollection.BuildServiceProvider();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
    }
}
