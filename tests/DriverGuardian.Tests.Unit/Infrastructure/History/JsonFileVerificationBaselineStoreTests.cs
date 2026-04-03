using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Infrastructure.History;

namespace DriverGuardian.Tests.Unit.Infrastructure.History;

public sealed class JsonFileVerificationBaselineStoreTests
{
    [Fact]
    public async Task SaveAllAndGetAllAsync_ShouldRoundTrip()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dg-verification-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonFileVerificationBaselineStore(filePath);
            var snapshots = new[]
            {
                new VerificationBaselineSnapshot(
                    new InstalledDriverSnapshot(
                        new DeviceIdentity("PCI\\VEN_1234&DEV_9999"),
                        new HardwareIdentifier("PCI\\VEN_1234&DEV_9999"),
                        "1.0.0",
                        null,
                        "Vendor"),
                    DateTimeOffset.UtcNow)
            };

            await store.SaveAllAsync(snapshots, CancellationToken.None);
            var loaded = await store.GetAllAsync(CancellationToken.None);

            Assert.Single(loaded);
            Assert.Equal("PCI\\VEN_1234&DEV_9999", loaded.First().Snapshot.DeviceIdentity.InstanceId);
            Assert.Equal("1.0.0", loaded.First().Snapshot.DriverVersion);
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
