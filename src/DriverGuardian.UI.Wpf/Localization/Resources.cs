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
    public static string LastScan_Summary_Format => GetString(nameof(LastScan_Summary_Format));

    private static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
