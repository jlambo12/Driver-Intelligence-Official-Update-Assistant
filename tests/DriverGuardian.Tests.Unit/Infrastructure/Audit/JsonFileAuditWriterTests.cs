using DriverGuardian.Infrastructure.Audit;

namespace DriverGuardian.Tests.Unit.Infrastructure.Audit;

public sealed class JsonFileAuditWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldPersistJsonlEntries()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dg-audit-{Guid.NewGuid():N}.jsonl");
        try
        {
            var writer = new JsonFileAuditWriter(filePath, maxEntries: 2);

            await writer.WriteAsync("entry-1", CancellationToken.None);
            await writer.WriteAsync("entry-2", CancellationToken.None);
            await writer.WriteAsync("entry-3", CancellationToken.None);

            var lines = await File.ReadAllLinesAsync(filePath, CancellationToken.None);
            Assert.Equal(2, lines.Length);
            Assert.Contains("entry-2", lines[0]);
            Assert.Contains("entry-3", lines[1]);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
