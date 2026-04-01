using System.Globalization;
using System.Resources;

namespace DriverGuardian.UI.Wpf.Localization;

public static class Resources
{
    private static readonly ResourceManager ResourceManager = new("DriverGuardian.UI.Wpf.Localization.Resources", typeof(Resources).Assembly);

    public static string MainWindow_Title => GetString(nameof(MainWindow_Title));
    public static string Status_Ready => GetString(nameof(Status_Ready));
    public static string Status_Scanning => GetString(nameof(Status_Scanning));
    public static string Scan_Action => GetString(nameof(Scan_Action));
    public static string Scan_Summary_Format => GetString(nameof(Scan_Summary_Format));
    public static string Discovery_Inspection_Summary_Format => GetString(nameof(Discovery_Inspection_Summary_Format));
    public static string Recommendation_Summary_Format => GetString(nameof(Recommendation_Summary_Format));
    public static string Manual_Handoff_Summary_Format => GetString(nameof(Manual_Handoff_Summary_Format));
    public static string Verification_Summary_Format => GetString(nameof(Verification_Summary_Format));

    private static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
