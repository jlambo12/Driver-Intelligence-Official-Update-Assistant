namespace DriverGuardian.Contracts.Normalization;

public interface INormalizationMapper
{
    string NormalizeHardwareId(string sourceValue);
}
