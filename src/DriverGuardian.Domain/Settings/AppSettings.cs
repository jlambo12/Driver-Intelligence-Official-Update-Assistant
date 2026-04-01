namespace DriverGuardian.Domain.Settings;

public sealed record AppSettings(bool AnalysisModeOnly, string UiCulture)
{
    public static AppSettings Default => new(true, "ru-RU");
}
