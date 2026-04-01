namespace DriverGuardian.UI.Wpf.Models;

public sealed record RecommendationDetailPresentation(
    string Title,
    string DeviceSummary,
    string Summary,
    string InstalledDriverSummary,
    string CandidateSummary,
    string ManualHandoffSummary,
    string ManualActionSummary,
    string VerificationSummary,
    string ManualStepGuidance,
    string VerificationResultSummary,
    string NextStepGuidance);
