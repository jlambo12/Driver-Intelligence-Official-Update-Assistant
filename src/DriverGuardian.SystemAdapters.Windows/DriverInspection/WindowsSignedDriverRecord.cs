namespace DriverGuardian.SystemAdapters.Windows.DriverInspection;

public sealed record WindowsSignedDriverRecord(
    string? InstanceId,
    string? DriverVersion,
    DateOnly? DriverDate,
    string? ProviderName);
