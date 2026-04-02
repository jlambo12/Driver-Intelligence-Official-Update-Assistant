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
    string? CustomFolderPath)
{
    public static DiagnosticLoggingPreferences Default => new(
        Enabled: true,
        CustomFolderPath: null);

    public DiagnosticLoggingPreferences Normalize()
    {
        var customPath = string.IsNullOrWhiteSpace(CustomFolderPath)
            ? null
            : CustomFolderPath.Trim();

        return this with { CustomFolderPath = customPath };
    }
}

public sealed record AppSettings(
    LocalizationPreferences Localization,
    HistoryPreferences History,
    ReportPreferences Reports,
    WorkflowGuidancePreferences WorkflowGuidance,
    SafetyPreferences Safety,
    DiagnosticLoggingPreferences DiagnosticLogging)
{
    public static AppSettings Default => new(
        Localization: LocalizationPreferences.Default,
        History: HistoryPreferences.Default,
        Reports: ReportPreferences.Default,
        WorkflowGuidance: WorkflowGuidancePreferences.Default,
        Safety: SafetyPreferences.Default,
        DiagnosticLogging: DiagnosticLoggingPreferences.Default);

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
            DiagnosticLogging = (DiagnosticLogging ?? DiagnosticLoggingPreferences.Default).Normalize()
        };
    }
}
