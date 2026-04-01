namespace DriverGuardian.Application.History.Models;

public sealed record ScanHistoryEntry : ResultHistoryEntry
{
    private ScanHistoryEntry(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        int discoveredDeviceCount,
        int inspectedDriverCount)
        : base(id, occurredAtUtc)
    {
        ScanSessionId = scanSessionId;
        DiscoveredDeviceCount = discoveredDeviceCount;
        InspectedDriverCount = inspectedDriverCount;
    }

    public Guid ScanSessionId { get; }

    public int DiscoveredDeviceCount { get; }

    public int InspectedDriverCount { get; }

    public static ScanHistoryEntry Create(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        int discoveredDeviceCount,
        int inspectedDriverCount)
    {
        if (scanSessionId == Guid.Empty)
        {
            throw new ArgumentException("Scan session identifier cannot be empty.", nameof(scanSessionId));
        }

        if (discoveredDeviceCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discoveredDeviceCount), "Discovered device count cannot be negative.");
        }

        if (inspectedDriverCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inspectedDriverCount), "Inspected driver count cannot be negative.");
        }

        return new ScanHistoryEntry(
            id,
            occurredAtUtc,
            scanSessionId,
            discoveredDeviceCount,
            inspectedDriverCount);
    }
}
