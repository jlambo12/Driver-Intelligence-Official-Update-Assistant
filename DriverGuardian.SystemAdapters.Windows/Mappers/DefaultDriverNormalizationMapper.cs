using DriverGuardian.Contracts.Abstractions;
using DriverGuardian.Contracts.Models;
using DriverGuardian.Domain.Entities;
using DriverGuardian.Domain.Enums;
using DriverGuardian.Domain.ValueObjects;

namespace DriverGuardian.SystemAdapters.Windows.Mappers;

public sealed class DefaultDriverNormalizationMapper : IDriverNormalizationMapper
{
    public NormalizedDriverRecord Map(DeviceInfo device, DriverMetadata metadata)
    {
        var snapshot = new InstalledDriverSnapshot(
            device.Identity,
            device.HardwareIds,
            metadata.Version,
            metadata.ReleaseDate,
            metadata.Provider,
            DriverSourceProvenance.Unknown,
            new CompatibilityConfidence(CompatibilityConfidenceLevel.Unknown, 0m),
            metadata.IsSigned,
            metadata.SignatureIssuer);

        return new NormalizedDriverRecord(snapshot, ["STUB_NORMALIZATION"]);
    }
}
