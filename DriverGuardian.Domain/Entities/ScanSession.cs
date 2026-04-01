namespace DriverGuardian.Domain.Entities;

public sealed record ScanSession(
    Guid SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyCollection<InstalledDriverSnapshot> Snapshots,
    bool HasErrors);
