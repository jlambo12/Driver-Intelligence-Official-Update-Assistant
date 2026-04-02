using DriverGuardian.Infrastructure.DiagnosticLogging;

namespace DriverGuardian.Tests.Unit.Infrastructure.DiagnosticLogging;

public sealed class FileDiagnosticLoggerTests
{
    [Fact]
    public async Task LogInfoAsync_ShouldWriteExpectedEntry()
    {
        var logsDirectory = Path.Combine(Path.GetTempPath(), $"driverguardian-logs-{Guid.NewGuid():N}");
        var logger = new FileDiagnosticLogger(logsDirectory);

        try
        {
            await logger.LogInfoAsync("scan.test", "info message", CancellationToken.None);

            var filePath = Directory.GetFiles(logsDirectory, "scan-*.log").Single();
            var content = await File.ReadAllTextAsync(filePath, CancellationToken.None);

            Assert.Contains("[INFO]", content, StringComparison.Ordinal);
            Assert.Contains("[scan.test]", content, StringComparison.Ordinal);
            Assert.Contains("info message", content, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(logsDirectory))
            {
                Directory.Delete(logsDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task LogErrorAsync_ShouldWriteExceptionContext()
    {
        var logsDirectory = Path.Combine(Path.GetTempPath(), $"driverguardian-logs-{Guid.NewGuid():N}");
        var logger = new FileDiagnosticLogger(logsDirectory);

        try
        {
            await logger.LogErrorAsync("scan.error", "failed", new InvalidOperationException("boom"), CancellationToken.None);

            var filePath = Directory.GetFiles(logsDirectory, "scan-*.log").Single();
            var content = await File.ReadAllTextAsync(filePath, CancellationToken.None);

            Assert.Contains("[ERROR]", content, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException", content, StringComparison.Ordinal);
            Assert.Contains("boom", content, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(logsDirectory))
            {
                Directory.Delete(logsDirectory, recursive: true);
            }
        }
    }
}
