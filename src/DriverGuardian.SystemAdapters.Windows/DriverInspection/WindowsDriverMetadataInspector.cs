using System.Management;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Contracts.DriverInspection;
using DriverGuardian.Domain.Drivers;

namespace DriverGuardian.SystemAdapters.Windows.DriverInspection;

public sealed class WindowsDriverMetadataInspector : IDriverMetadataInspector
{
    private const string Query = "SELECT DeviceID, DriverVersion, DriverDate, DriverProviderName FROM Win32_PnPSignedDriver";

    public Task<DriverInspectionResult> InspectAsync(
        IReadOnlyCollection<DiscoveredDevice> devices,
        CancellationToken cancellationToken)
    {
        if (devices.Count == 0)
        {
            return Task.FromResult(new DriverInspectionResult(DriverInspectionStatus.Completed, [], []));
        }

        var devicesByInstanceId = devices
            .GroupBy(d => d.Identity.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var issues = new List<ScanIssue>();
        try
        {
            using var searcher = new ManagementObjectSearcher(Query);
            using var collection = searcher.Get();

            var snapshots = new List<InstalledDriverSnapshot>();
            var hasSkippedEntries = false;

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
                    hasSkippedEntries = true;
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

            if (hasSkippedEntries)
            {
                issues.Add(new ScanIssue("inspection", "entry_parse_error", "Часть записей драйверов не удалось разобрать во время inspection."));
            }

            var status = hasSkippedEntries ? DriverInspectionStatus.Partial : DriverInspectionStatus.Completed;
            return Task.FromResult(new DriverInspectionResult(status, snapshots, issues));
        }
        catch (ManagementException ex)
        {
            issues.Add(new ScanIssue("inspection", "wmi_management_error", ex.Message));
            return Task.FromResult(new DriverInspectionResult(DriverInspectionStatus.Failed, [], issues));
        }
        catch (PlatformNotSupportedException ex)
        {
            issues.Add(new ScanIssue("inspection", "platform_not_supported", ex.Message));
            return Task.FromResult(new DriverInspectionResult(DriverInspectionStatus.Failed, [], issues));
        }
        catch (UnauthorizedAccessException ex)
        {
            issues.Add(new ScanIssue("inspection", "access_denied", ex.Message));
            return Task.FromResult(new DriverInspectionResult(DriverInspectionStatus.Failed, [], issues));
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
