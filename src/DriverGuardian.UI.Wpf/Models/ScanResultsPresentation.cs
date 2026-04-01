using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record ScanResultsPresentation(
    string ScanSummary,
    string DiscoveryAndInspectionSummary,
    string RecommendationSummary,
    string ManualHandoffSummary,
    string VerificationSummary,
    string ActionFlowTitle,
    string SafetyNotice,
    IReadOnlyCollection<UserGuidedActionStepPresentation> UserGuidedSteps)
{
    public static ScanResultsPresentation Empty() =>
        new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<UserGuidedActionStepPresentation>());

    public static ScanResultsPresentation FromResult(MainScreenWorkflowResult result)
    {
        var hasRecommendation = result.RecommendedCount > 0;
        var hasReadyHandoff = result.ManualHandoffReadyCount > 0;

        return new ScanResultsPresentation(
            ScanSummary: string.Format(UiStrings.ScanSummaryFormat, result.ScanSessionId, result.UiCulture, result.ProviderCount),
            DiscoveryAndInspectionSummary: string.Format(
                UiStrings.DiscoveryInspectionSummaryFormat,
                result.DiscoveredDeviceCount,
                result.InspectedDriverCount),
            RecommendationSummary: string.Format(
                UiStrings.RecommendationSummaryFormat,
                result.RecommendedCount,
                result.NotRecommendedCount),
            ManualHandoffSummary: string.Format(
                UiStrings.ManualHandoffSummaryFormat,
                result.ManualHandoffReadyCount,
                result.ManualHandoffUserActionCount),
            VerificationSummary: string.Format(UiStrings.VerificationSummaryFormat, result.VerificationSummary),
            ActionFlowTitle: UiStrings.ActionFlowTitle,
            SafetyNotice: UiStrings.ActionFlowSafetyNotice,
            UserGuidedSteps: BuildUserGuidedSteps(hasRecommendation, hasReadyHandoff));
    }

    private static IReadOnlyCollection<UserGuidedActionStepPresentation> BuildUserGuidedSteps(bool hasRecommendation, bool hasReadyHandoff)
    {
        var reviewStatus = hasRecommendation ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusNotAvailable;
        var handoffStatus = hasReadyHandoff ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked;
        var manualInstallStatus = hasRecommendation ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusWait;
        var verificationStatus = hasRecommendation ? UiStrings.ActionStatusReturn : UiStrings.ActionStatusWait;

        return
        [
            new UserGuidedActionStepPresentation(UiStrings.ActionStepReviewRecommendation, reviewStatus, UiStrings.ActionStepReviewRecommendationHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepOpenOfficialSource, handoffStatus, UiStrings.ActionStepOpenOfficialSourceHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepDownloadManually, manualInstallStatus, UiStrings.ActionStepDownloadManuallyHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepInstallOutsideApp, manualInstallStatus, UiStrings.ActionStepInstallOutsideAppHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepReturnForVerification, verificationStatus, UiStrings.ActionStepReturnForVerificationHint)
        ];
    }
}
