namespace DriverGuardian.Domain.Scanning;

public sealed record ScanSession(Guid Id, DateTimeOffset StartedAtUtc, DateTimeOffset? CompletedAtUtc)
{
    public static ScanSession Start(Guid id, DateTimeOffset startedAtUtc) => new(id, startedAtUtc, null);

    public ScanSession Complete(DateTimeOffset completedAtUtc)
    {
        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Completion time cannot be before start time.", nameof(completedAtUtc));
        }

        return this with { CompletedAtUtc = completedAtUtc };
    }
}
