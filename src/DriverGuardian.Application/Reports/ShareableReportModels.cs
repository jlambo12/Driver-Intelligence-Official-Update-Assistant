using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Downloads;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Recommendations;

namespace DriverGuardian.Application.Reports;

public sealed record ShareableReportRequest(
    ScanResult ScanResult,
    IReadOnlyCollection<RecommendationSummary> Recommendations,
    IReadOnlyCollection<ManualInstallHandoffReportItem> ManualInstallHandoffs,
    IReadOnlyCollection<VerificationReportItem> Verifications,
    DateTimeOffset GeneratedAtUtc);

public sealed record ManualInstallHandoffReportItem(
    DeviceIdentity DeviceIdentity,
    ManualInstallHandoffDecision Decision);

public sealed record VerificationReportItem(
    DeviceIdentity DeviceIdentity,
    PostInstallVerificationResult Result);

public sealed record ShareableReport(
    ReportMetadata Metadata,
    ScanSummarySection ScanSummary,
    RecommendationSummarySection RecommendationSummary,
    ManualInstallHandoffSummarySection ManualInstallHandoffSummary,
    VerificationSummarySection VerificationSummary,
    IReadOnlyCollection<DeviceReportSection> Devices);

public sealed record ReportMetadata(
    Guid ScanSessionId,
    DateTimeOffset ScanStartedAtUtc,
    DateTimeOffset? ScanCompletedAtUtc,
    DateTimeOffset GeneratedAtUtc);

public sealed record ScanSummarySection(int TotalDevices);

public sealed record RecommendationSummarySection(
    int TotalRecommendations,
    int RecommendedCount,
    int NotRecommendedCount);

public sealed record ManualInstallHandoffSummarySection(
    int TotalHandoffs,
    int ReadyCount,
    int RequiresActionCount,
    int NotReadyCount);

public sealed record VerificationSummarySection(
    int TotalVerifications,
    int VerifiedChangedCount,
    int PartialCount,
    int NoChangeCount,
    int DeviceMissingCount,
    int InsufficientEvidenceCount);

public sealed record DeviceReportSection(
    string DeviceDisplayName,
    string DeviceInstanceId,
    ScanDriverSnapshotSection DriverSnapshot,
    RecommendationReportSection? Recommendation,
    ManualInstallHandoffReportSection? ManualInstallHandoff,
    VerificationReportSection? Verification);

public sealed record ScanDriverSnapshotSection(
    string DriverVersion,
    string? DriverDate,
    string? ProviderName,
    string HardwareId);

public sealed record RecommendationReportSection(
    bool HasRecommendation,
    string Reason,
    string? RecommendedVersion);

public sealed record ManualInstallHandoffReportSection(
    string Outcome,
    bool IsReady,
    string? PackageUri,
    IReadOnlyCollection<string> Reasons);

public sealed record VerificationReportSection(
    string Outcome,
    string Reason,
    bool IsVerifiedChanged,
    string Message,
    IReadOnlyCollection<string> Differences);
