using DriverGuardian.Contracts.Models;

namespace DriverGuardian.SystemAdapters.Windows.Abstractions;

public interface IWindowsDeviceQuery
{
    Task<IReadOnlyCollection<DeviceInfo>> QueryAsync(CancellationToken cancellationToken);
}
