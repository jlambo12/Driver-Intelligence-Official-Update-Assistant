namespace DriverGuardian.Domain.Entities;

public sealed record AppSettings(
    string Locale,
    bool EnableTelemetry,
    bool StrictOfficialSourceOnly,
    int MaxScanConcurrency,
    DateTimeOffset UpdatedAtUtc)
{
    public static AppSettings Default => new(
        Locale: "ru-RU",
        EnableTelemetry: false,
        StrictOfficialSourceOnly: true,
        MaxScanConcurrency: 2,
        UpdatedAtUtc: DateTimeOffset.UtcNow);
}
