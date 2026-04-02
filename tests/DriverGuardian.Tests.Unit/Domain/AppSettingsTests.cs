using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Tests.Unit.Domain;

public sealed class AppSettingsTests
{
    [Fact]
    public void Default_ShouldRemainSafetyFirst()
    {
        var settings = AppSettings.Default;

        Assert.True(settings.AnalysisModeOnly);
        Assert.True(settings.Safety.RequireOfficialSourceReviewBeforeDownload);
        Assert.True(settings.Safety.BlockAutomaticInstallExecution);
    }

    [Fact]
    public void Normalize_ShouldApplyFallbacksAndBounds()
    {
        var settings = new AppSettings(
            Localization: new LocalizationPreferences("  en-US  "),
            History: new HistoryPreferences(5, HistoryRetentionStrategy.KeepMostRecent),
            Reports: ReportPreferences.Default,
            WorkflowGuidance: WorkflowGuidancePreferences.Default,
            Safety: SafetyPreferences.Default,
            DiagnosticLogging: new DiagnosticLoggingPreferences(true, "  C:\\Logs  "));

        var normalized = settings.Normalize();

        Assert.Equal("en-US", normalized.UiCulture);
        Assert.Equal(10, normalized.History.MaxEntries);
        Assert.Equal("C:\\Logs", normalized.DiagnosticLogging.CustomFolderPath);
    }
}
