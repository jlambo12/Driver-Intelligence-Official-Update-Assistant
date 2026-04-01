using DriverGuardian.Contracts.Models;

namespace DriverGuardian.Contracts.Abstractions;

public interface IDriverNormalizationMapper
{
    NormalizedDriverRecord Map(DeviceInfo device, DriverMetadata metadata);
}
