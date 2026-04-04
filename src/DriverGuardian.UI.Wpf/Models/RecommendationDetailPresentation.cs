namespace DriverGuardian.UI.Wpf.Models;

public sealed record RecommendationDetailPresentation(
    string Title,
    string WorkflowState,
    string WorkflowStateHint,
    string DeviceSummary,
    string Summary,
    string InstalledDriverSummary,
    string CandidateSummary,
    string OfficialSourceSummary,
    string? OfficialSourceUrl,
    bool CanOpenOfficialSourceUrl,
    string OfficialSourceActionHint,
    string ManualHandoffSummary,
    string ManualActionSummary,
    string VerificationSummary,
    string VerificationStatus,
    string NextStepGuidance);
