using System.Management;
using DriverGuardian.Contracts.DeviceDiscovery;

namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;

public sealed class WindowsDeviceDiscoveryService : IDeviceDiscoveryService
{
    private const string Query = "SELECT DeviceID, Name, PNPDeviceID, HardwareID, Manufacturer, PNPClass, ConfigManagerErrorCode, Status FROM Win32_PnPEntity";

    public Task<IReadOnlyCollection<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(Query);
            using var collection = searcher.Get();

            var devices = new List<DiscoveredDevice>();

            foreach (ManagementObject entity in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var snapshot = WindowsPnpEntitySnapshot.From(entity);
                if (string.IsNullOrWhiteSpace(snapshot.InstanceId))
                {
                    continue;
                }

                devices.Add(WindowsDeviceDiscoveryMapper.Map(snapshot));
            }

            return Task.FromResult<IReadOnlyCollection<DiscoveredDevice>>(devices);
        }
        catch (ManagementException)
        {
            return Task.FromResult<IReadOnlyCollection<DiscoveredDevice>>([]);
        }
        catch (PlatformNotSupportedException)
        {
            return Task.FromResult<IReadOnlyCollection<DiscoveredDevice>>([]);
        }
    }
}
