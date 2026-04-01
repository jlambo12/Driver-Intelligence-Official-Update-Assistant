using System.Management;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.SystemAdapters.Windows.DriverInspection;

public sealed class WindowsDriverMetadataInspectorStub : IDriverMetadataInspector
{
    private const string Query = "SELECT DeviceID, DriverVersion, DriverDate, DriverProviderName FROM Win32_PnPSignedDriver";

    public Task<IReadOnlyCollection<InstalledDriverSnapshot>> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken)
    {
        if (devices.Count == 0)
        {
            return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
        }

        var devicesByInstanceId = devices
            .GroupBy(d => d.Identity.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher(Query);
            using var collection = searcher.Get();

            var snapshots = new List<InstalledDriverSnapshot>();

            foreach (ManagementObject entry in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();

                WindowsSignedDriverRecord record;
                try
                {
                    record = WindowsSignedDriverRecordFactory.From(entry);
                }
                catch (ManagementException)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(record.InstanceId)
                    || !devicesByInstanceId.TryGetValue(record.InstanceId, out var device))
                {
                    continue;
                }

                var mapped = WindowsDriverMetadataMapper.Map(device, record);
                if (mapped is not null)
                {
                    snapshots.Add(mapped);
                }
            }

            return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>(snapshots);
        }
        catch (ManagementException)
        {
            return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
        }
        catch (PlatformNotSupportedException)
        {
            return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<IReadOnlyCollection<InstalledDriverSnapshot>>([]);
        }
    }
}

internal static class WindowsSignedDriverRecordFactory
{
    public static WindowsSignedDriverRecord From(ManagementObject entry)
    {
        var rawDriverDate = entry["DriverDate"] as string;

        return new WindowsSignedDriverRecord(
            InstanceId: entry["DeviceID"] as string,
            DriverVersion: entry["DriverVersion"] as string,
            DriverDate: ParseDate(rawDriverDate),
            ProviderName: entry["DriverProviderName"] as string);
    }

    private static DateOnly? ParseDate(string? rawDriverDate)
    {
        if (string.IsNullOrWhiteSpace(rawDriverDate))
        {
            return null;
        }

        try
        {
            var dateTime = ManagementDateTimeConverter.ToDateTime(rawDriverDate);
            return DateOnly.FromDateTime(dateTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
