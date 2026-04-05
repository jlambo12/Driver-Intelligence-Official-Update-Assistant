using DriverGuardian.Infrastructure.Audit;

namespace DriverGuardian.Tests.Unit.Infrastructure.Audit;

public sealed class JsonFileAuditWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldRotateAfterMaxEntriesPerFile()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), $"dg-audit-{Guid.NewGuid():N}");
        var filePath = Path.Combine(folderPath, "audit.jsonl");
        try
        {
            Directory.CreateDirectory(folderPath);
            var writer = new JsonFileAuditWriter(filePath, maxEntries: 2, maxArchiveFiles: 5);

            await writer.WriteAsync("entry-1", CancellationToken.None);
            await writer.WriteAsync("entry-2", CancellationToken.None);
            await writer.WriteAsync("entry-3", CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(filePath, CancellationToken.None);
            Assert.Single(lines);
            Assert.Contains("entry-3", lines[0]);

            var archives = GetArchiveFiles(folderPath);
            Assert.Single(archives);

            var archiveLines = await File.ReadAllLinesAsync(archives[0], CancellationToken.None);
            Assert.Equal(2, archiveLines.Length);
            Assert.Contains("entry-1", archiveLines[0]);
            Assert.Contains("entry-2", archiveLines[1]);
        }
        finally
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_ShouldKeepOnlyConfiguredArchiveCount()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), $"dg-audit-{Guid.NewGuid():N}");
        var filePath = Path.Combine(folderPath, "audit.jsonl");
        try
        {
            Directory.CreateDirectory(folderPath);
            var writer = new JsonFileAuditWriter(filePath, maxEntries: 1, maxArchiveFiles: 2);

            await writer.WriteAsync("entry-1", CancellationToken.None);
            await writer.WriteAsync("entry-2", CancellationToken.None);
            await writer.WriteAsync("entry-3", CancellationToken.None);
            await writer.WriteAsync("entry-4", CancellationToken.None);

            var archives = GetArchiveFiles(folderPath);
            Assert.Equal(2, archives.Length);

            var activeLines = await File.ReadAllLinesAsync(filePath, CancellationToken.None);
            Assert.Single(activeLines);
            Assert.Contains("entry-4", activeLines[0]);
        }
        finally
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_ShouldDeleteAllArchives_WhenRetentionIsZero()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), $"dg-audit-{Guid.NewGuid():N}");
        var filePath = Path.Combine(folderPath, "audit.jsonl");
        try
        {
            Directory.CreateDirectory(folderPath);
            var writer = new JsonFileAuditWriter(filePath, maxEntries: 1, maxArchiveFiles: 0);

            await writer.WriteAsync("entry-1", CancellationToken.None);
            await writer.WriteAsync("entry-2", CancellationToken.None);
            await writer.WriteAsync("entry-3", CancellationToken.None);

            var archives = GetArchiveFiles(folderPath);
            Assert.Empty(archives);
        }
        finally
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
            }
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenMaxArchiveFilesIsNegative()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dg-audit-{Guid.NewGuid():N}.jsonl");
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JsonFileAuditWriter(filePath, maxEntries: 1, maxArchiveFiles: -1));
    }

    private static string[] GetArchiveFiles(string folderPath)
        => Directory
            .GetFiles(folderPath, "audit.jsonl*")
            .Where(path => Path.GetFileName(path).StartsWith("audit.jsonl.", StringComparison.Ordinal))
            .ToArray();
}
