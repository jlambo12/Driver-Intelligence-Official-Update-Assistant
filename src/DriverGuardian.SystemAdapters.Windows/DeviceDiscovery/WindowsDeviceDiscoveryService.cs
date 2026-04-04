using System.Management;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.SystemAdapters.Windows.DeviceDiscovery;

public sealed class WindowsDeviceDiscoveryService : IDeviceDiscoveryService
{
    private const string Query = "SELECT DeviceID, Name, PNPDeviceID, HardwareID, Manufacturer, PNPClass, ConfigManagerErrorCode, Status FROM Win32_PnPEntity";

    private readonly DeviceScanProfile _profile;

    public WindowsDeviceDiscoveryService(DeviceScanProfile profile = DeviceScanProfile.Balanced)
    {
        _profile = profile;
    }

    public Task<DeviceDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        var issues = new List<ScanIssue>();
        try
        {
            using var searcher = new ManagementObjectSearcher(Query);
            using var collection = searcher.Get();

            var devices = new List<DiscoveredDevice>();
            var hasSkippedEntities = false;

            foreach (ManagementObject entity in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var snapshot = WindowsPnpEntitySnapshot.From(entity);
                    if (!WindowsDeviceInclusionPolicy.ShouldInclude(snapshot, _profile))
                    {
                        continue;
                    }

                    devices.Add(WindowsDeviceDiscoveryMapper.Map(snapshot));
                }
                catch (ManagementException)
                {
                    // skip malformed entity and continue collecting partial results
                    hasSkippedEntities = true;
                }
            }

            if (hasSkippedEntities)
            {
                issues.Add(new ScanIssue("discovery", "entity_parse_error", "Часть устройств не удалось разобрать во время discovery."));
            }

            var status = hasSkippedEntities ? DeviceDiscoveryStatus.Partial : DeviceDiscoveryStatus.Completed;
            return Task.FromResult(new DeviceDiscoveryResult(status, devices, issues));
        }
        catch (ManagementException ex)
        {
            issues.Add(new ScanIssue("discovery", "wmi_management_error", ex.Message));
            return Task.FromResult(new DeviceDiscoveryResult(DeviceDiscoveryStatus.Failed, [], issues));
        }
        catch (PlatformNotSupportedException ex)
        {
            issues.Add(new ScanIssue("discovery", "platform_not_supported", ex.Message));
            return Task.FromResult(new DeviceDiscoveryResult(DeviceDiscoveryStatus.Failed, [], issues));
        }
        catch (UnauthorizedAccessException ex)
        {
            issues.Add(new ScanIssue("discovery", "access_denied", ex.Message));
            return Task.FromResult(new DeviceDiscoveryResult(DeviceDiscoveryStatus.Failed, [], issues));
        }
    }
}
