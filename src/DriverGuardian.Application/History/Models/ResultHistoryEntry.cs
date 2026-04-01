namespace DriverGuardian.Application.History.Models;

public abstract record ResultHistoryEntry
{
    protected ResultHistoryEntry(Guid id, DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("History entry identifier cannot be empty.", nameof(id));
        }

        Id = id;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }

    public DateTimeOffset OccurredAtUtc { get; }
}
