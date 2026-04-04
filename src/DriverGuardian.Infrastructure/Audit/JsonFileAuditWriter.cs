using System.Text.Json;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Infrastructure.Audit;

public sealed class JsonFileAuditWriter(
    string filePath,
    int maxEntries = 2000,
    int maxArchiveFiles = 5) : IAuditWriter
{
    private readonly string _filePath = string.IsNullOrWhiteSpace(filePath)
        ? throw new ArgumentException("Audit file path is required.", nameof(filePath))
        : filePath;

    private readonly int _maxEntriesPerFile = maxEntries <= 0 ? 2000 : maxEntries;
    private readonly int _maxArchiveFiles = maxArchiveFiles < 0
        ? throw new ArgumentOutOfRangeException(nameof(maxArchiveFiles), "Archive file count cannot be negative.")
        : maxArchiveFiles;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private int _entriesInActiveFile = -1;

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

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureEntryCountInitializedAsync(cancellationToken);
            if (_entriesInActiveFile >= _maxEntriesPerFile)
            {
                await RotateAsync(cancellationToken);
            }

            var payload = JsonSerializer.Serialize(new StoredAuditEntry(DateTimeOffset.UtcNow, entry.Trim()));
            await using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(payload);
            await writer.FlushAsync(cancellationToken);
            _entriesInActiveFile++;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task EnsureEntryCountInitializedAsync(CancellationToken cancellationToken)
    {
        if (_entriesInActiveFile >= 0)
        {
            return;
        }

        if (!File.Exists(_filePath))
        {
            _entriesInActiveFile = 0;
            return;
        }

        var count = 0;
        await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is not null)
        {
            count++;
        }

        _entriesInActiveFile = count;
    }

    private Task RotateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            _entriesInActiveFile = 0;
            return Task.CompletedTask;
        }

        var archivePath = BuildArchivePath();
        File.Move(_filePath, archivePath);
        _entriesInActiveFile = 0;
        CleanupArchives(cancellationToken);
        return Task.CompletedTask;
    }

    private void CleanupArchives(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var fileName = Path.GetFileName(_filePath);
        var archivePrefix = $"{fileName}.";
        var archives = Directory
            .EnumerateFiles(directory, $"{fileName}.*")
            .Where(path => Path.GetFileName(path).StartsWith(archivePrefix, StringComparison.Ordinal))
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var staleArchive in archives.Skip(_maxArchiveFiles))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(staleArchive);
        }
    }

    private string BuildArchivePath()
    {
        var suffix = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssffff}-{Guid.NewGuid():N}";
        return $"{_filePath}.{suffix}";
    }

    private sealed record StoredAuditEntry(DateTimeOffset OccurredAtUtc, string Entry);
}
