namespace DriverGuardian.Domain.Settings;

public enum HistoryRetentionStrategy
{
    KeepMostRecent = 0,
    KeepUntilManualClear = 1
}

public enum ShareableReportFormat
{
    Markdown = 0,
    PlainText = 1
}

public enum DeviceScanProfile
{
    Minimal = 0,
    Balanced = 1,
    Comprehensive = 2
}

public sealed record LocalizationPreferences(string PreferredCulture)
{
    public static LocalizationPreferences Default => new("ru-RU");

    public LocalizationPreferences Normalize()
    {
        return string.IsNullOrWhiteSpace(PreferredCulture)
            ? Default
            : this with { PreferredCulture = PreferredCulture.Trim() };
    }
}

public sealed record HistoryPreferences(int MaxEntries, HistoryRetentionStrategy RetentionStrategy)
{
    public static HistoryPreferences Default => new(50, HistoryRetentionStrategy.KeepMostRecent);

    public HistoryPreferences Normalize()
    {
        var normalizedCount = Math.Clamp(MaxEntries, 10, 500);
        return this with { MaxEntries = normalizedCount };
    }
}

public sealed record ReportPreferences(
    ShareableReportFormat DefaultFormat,
    bool IncludeScanTimestampUtc,
    bool IncludeOfficialSourceLinks)
{
    public static ReportPreferences Default => new(
        DefaultFormat: ShareableReportFormat.Markdown,
        IncludeScanTimestampUtc: true,
        IncludeOfficialSourceLinks: true);
}

public sealed record WorkflowGuidancePreferences(
    bool ShowManualInstallReminders,
    bool ShowPostInstallVerificationHints,
    bool PreferGroupedRecommendations)
{
    public static WorkflowGuidancePreferences Default => new(
        ShowManualInstallReminders: true,
        ShowPostInstallVerificationHints: true,
        PreferGroupedRecommendations: true);
}

public sealed record SafetyPreferences(
    bool AnalysisModeOnly,
    bool RequireOfficialSourceReviewBeforeDownload,
    bool BlockAutomaticInstallExecution)
{
    public static SafetyPreferences Default => new(
        AnalysisModeOnly: true,
        RequireOfficialSourceReviewBeforeDownload: true,
        BlockAutomaticInstallExecution: true);
}

public sealed record DiagnosticLoggingPreferences(
    bool Enabled,
    string? CustomLogsFolderPath)
{
    public static DiagnosticLoggingPreferences Default => new(
        Enabled: true,
        CustomLogsFolderPath: null);

    public DiagnosticLoggingPreferences Normalize()
    {
        if (string.IsNullOrWhiteSpace(CustomLogsFolderPath))
        {
            return this with { CustomLogsFolderPath = null };
        }

        return this with { CustomLogsFolderPath = CustomLogsFolderPath.Trim() };
    }
}

public sealed record ScanCoveragePreferences(DeviceScanProfile DeviceProfile)
{
    public static ScanCoveragePreferences Default => new(DeviceScanProfile.Balanced);
}

public sealed record AppSettings(
    LocalizationPreferences Localization,
    HistoryPreferences History,
    ReportPreferences Reports,
    WorkflowGuidancePreferences WorkflowGuidance,
    SafetyPreferences Safety,
    DiagnosticLoggingPreferences DiagnosticLogging,
    ScanCoveragePreferences ScanCoverage)
{
    public static AppSettings Default => new(
        Localization: LocalizationPreferences.Default,
        History: HistoryPreferences.Default,
        Reports: ReportPreferences.Default,
        WorkflowGuidance: WorkflowGuidancePreferences.Default,
        Safety: SafetyPreferences.Default,
        DiagnosticLogging: DiagnosticLoggingPreferences.Default,
        ScanCoverage: ScanCoveragePreferences.Default);

    public string UiCulture => Localization.PreferredCulture;

    public bool AnalysisModeOnly => Safety.AnalysisModeOnly;

    public AppSettings Normalize()
    {
        return this with
        {
            Localization = (Localization ?? LocalizationPreferences.Default).Normalize(),
            History = (History ?? HistoryPreferences.Default).Normalize(),
            Reports = Reports ?? ReportPreferences.Default,
            WorkflowGuidance = WorkflowGuidance ?? WorkflowGuidancePreferences.Default,
            Safety = Safety ?? SafetyPreferences.Default,
            DiagnosticLogging = (DiagnosticLogging ?? DiagnosticLoggingPreferences.Default).Normalize(),
            ScanCoverage = ScanCoverage ?? ScanCoveragePreferences.Default
        };
    }
}
