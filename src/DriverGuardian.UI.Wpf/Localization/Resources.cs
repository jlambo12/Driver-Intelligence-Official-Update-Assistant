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
    public static string Action_Flow_Title => GetString(nameof(Action_Flow_Title));
    public static string Action_Flow_Safety_Notice => GetString(nameof(Action_Flow_Safety_Notice));
    public static string Action_Status_Required => GetString(nameof(Action_Status_Required));
    public static string Action_Status_Available => GetString(nameof(Action_Status_Available));
    public static string Action_Status_Blocked => GetString(nameof(Action_Status_Blocked));
    public static string Action_Status_Wait => GetString(nameof(Action_Status_Wait));
    public static string Action_Status_Not_Available => GetString(nameof(Action_Status_Not_Available));
    public static string Action_Status_Return => GetString(nameof(Action_Status_Return));
    public static string Action_Step_Review_Recommendation => GetString(nameof(Action_Step_Review_Recommendation));
    public static string Action_Step_Review_Recommendation_Hint => GetString(nameof(Action_Step_Review_Recommendation_Hint));
    public static string Action_Step_Open_Official_Source => GetString(nameof(Action_Step_Open_Official_Source));
    public static string Action_Step_Open_Official_Source_Hint => GetString(nameof(Action_Step_Open_Official_Source_Hint));
    public static string Action_Step_Download_Manually => GetString(nameof(Action_Step_Download_Manually));
    public static string Action_Step_Download_Manually_Hint => GetString(nameof(Action_Step_Download_Manually_Hint));
    public static string Action_Step_Install_Outside_App => GetString(nameof(Action_Step_Install_Outside_App));
    public static string Action_Step_Install_Outside_App_Hint => GetString(nameof(Action_Step_Install_Outside_App_Hint));
    public static string Action_Step_Return_For_Verification => GetString(nameof(Action_Step_Return_For_Verification));
    public static string Action_Step_Return_For_Verification_Hint => GetString(nameof(Action_Step_Return_For_Verification_Hint));

    private static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
