namespace DriverGuardian.Application.History.Models;

public sealed record VerificationHistoryEntry : ResultHistoryEntry
{
    private VerificationHistoryEntry(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        VerificationHistoryStatus status,
        string? note)
        : base(id, occurredAtUtc)
    {
        ScanSessionId = scanSessionId;
        Status = status;
        Note = note;
    }

    public Guid ScanSessionId { get; }

    public VerificationHistoryStatus Status { get; }

    public string? Note { get; }

    public static VerificationHistoryEntry Create(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid scanSessionId,
        VerificationHistoryStatus status,
        string? note)
    {
        if (scanSessionId == Guid.Empty)
        {
            throw new ArgumentException("Scan session identifier cannot be empty.", nameof(scanSessionId));
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        return new VerificationHistoryEntry(id, occurredAtUtc, scanSessionId, status, normalizedNote);
    }
}
