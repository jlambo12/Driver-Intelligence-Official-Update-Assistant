namespace DriverGuardian.UI.Wpf.Models;

public sealed record VerificationReturnPresentation(
    string Title,
    string ReadinessSummary,
    string ManualConfirmationLabel,
    string ManualCompletionHint,
    string LastVerificationSummary,
    bool IsReady,
    bool RequiresManualConfirmation);
