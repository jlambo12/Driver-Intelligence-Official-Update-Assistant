using System.Text.Json;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Audit;

public sealed class JsonFileAuditWriter(string filePath, int maxEntries = 2000) : IAuditWriter
{
    private readonly string _filePath = string.IsNullOrWhiteSpace(filePath)
        ? throw new ArgumentException("Audit file path is required.", nameof(filePath))
        : filePath;

    private readonly int _maxEntries = maxEntries <= 0 ? 2000 : maxEntries;

    public async Task WriteAsync(string entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = File.Exists(_filePath)
            ? await File.ReadAllLinesAsync(_filePath, cancellationToken)
            : [];

        var payload = JsonSerializer.Serialize(new StoredAuditEntry(DateTimeOffset.UtcNow, entry.Trim()));
        var updated = lines.Concat([payload]).TakeLast(_maxEntries).ToArray();
        await File.WriteAllLinesAsync(_filePath, updated, cancellationToken);
    }

    private sealed record StoredAuditEntry(DateTimeOffset OccurredAtUtc, string Entry);
}
