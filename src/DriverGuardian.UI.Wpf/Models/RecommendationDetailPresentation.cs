namespace DriverGuardian.UI.Wpf.Models;

public sealed record RecommendationDetailPresentation(
    string Title,
    string DeviceSummary,
    string RecommendationStatus,
    string Summary,
    string InstalledDriverSummary,
    string CandidateSummary,
    string ManualHandoffSummary,
    string ManualActionSummary,
    string VerificationSummary,
    string VerificationStatus,
    string NextStepGuidance);
