using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record ScanResultsPresentation(
    string ScanSummary,
    string DiscoveryAndInspectionSummary,
    string RecommendationSummary,
    string ManualHandoffSummary,
    string VerificationSummary,
    string OfficialSourceSummary,
    string ActionFlowTitle,
    string SafetyNotice,
    VerificationReturnPresentation VerificationReturn,
    IReadOnlyCollection<RecommendationDetailPresentation> RecommendationDetails,
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
            string.Empty,
            new VerificationReturnPresentation(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, false),
            Array.Empty<RecommendationDetailPresentation>(),
            Array.Empty<UserGuidedActionStepPresentation>());

    public static ScanResultsPresentation FromResult(MainScreenWorkflowResult result, bool manualStepConfirmed)
    {
        var hasRecommendation = result.RecommendedCount > 0;
        var hasReadyHandoff = result.ManualHandoffReadyCount > 0;

        return new ScanResultsPresentation(
            ScanSummary: string.Format(UiStrings.ScanSummaryFormat, result.ScanSessionId, result.UiCulture, result.ProviderCount),
            DiscoveryAndInspectionSummary: string.Format(UiStrings.DiscoveryInspectionSummaryFormat, result.DiscoveredDeviceCount, result.InspectedDriverCount),
            RecommendationSummary: string.Format(UiStrings.RecommendationSummaryFormat, result.RecommendedCount, result.NotRecommendedCount),
            ManualHandoffSummary: string.Format(UiStrings.ManualHandoffSummaryFormat, result.ManualHandoffReadyCount, result.ManualHandoffUserActionCount),
            VerificationSummary: string.Format(UiStrings.VerificationSummaryFormat, result.VerificationSummary),
            OfficialSourceSummary: BuildOfficialSourceSummary(result.OfficialSourceAction),
            ActionFlowTitle: UiStrings.ActionFlowTitle,
            SafetyNotice: UiStrings.ActionFlowSafetyNotice,
            VerificationReturn: BuildVerificationReturnPresentation(result.VerificationReturn, manualStepConfirmed),
            RecommendationDetails: result.RecommendationDetails.Select(detail => MapDetail(detail, manualStepConfirmed)).ToArray(),
            UserGuidedSteps: BuildUserGuidedSteps(hasRecommendation, hasReadyHandoff, result.OfficialSourceAction.IsReady, manualStepConfirmed));
    }

    private static RecommendationDetailPresentation MapDetail(RecommendationDetailResult detail, bool manualStepConfirmed)
    {
        var recommendationTitle = detail.HasRecommendation
            ? UiStrings.RecommendationTitleRecommended
            : UiStrings.RecommendationTitleNotRecommended;
        var candidateSummary = detail.HasRecommendation && !string.IsNullOrWhiteSpace(detail.RecommendedVersion)
            ? string.Format(UiStrings.RecommendationCandidateVersionFormat, detail.RecommendedVersion)
            : UiStrings.RecommendationCandidateUnavailable;
        var reasonSummary = string.IsNullOrWhiteSpace(detail.RecommendationReason)
            ? UiStrings.RecommendationReasonUnavailable
            : detail.RecommendationReason;
        var nextStep = detail.HasRecommendation
            ? UiStrings.RecommendationNextStepRecommended
            : UiStrings.RecommendationNextStepDeferred;

        var manualStepGuidance = detail.HasRecommendation
            ? UiStrings.RecommendationManualStepGuidance
            : UiStrings.RecommendationManualStepNotRequired;
        var verificationResultSummary = detail.VerificationPending
            ? (manualStepConfirmed ? UiStrings.RecommendationVerificationReadyToRun : UiStrings.RecommendationVerificationPending)
            : UiStrings.RecommendationVerificationNotRequired;

        return new RecommendationDetailPresentation(
            recommendationTitle,
            string.Format(UiStrings.RecommendationDeviceFormat, detail.DeviceId),
            reasonSummary,
            string.Format(UiStrings.RecommendationInstalledDriverFormat, detail.InstalledVersion, detail.InstalledProvider ?? UiStrings.RecommendationProviderUnknown),
            candidateSummary,
            string.Format(UiStrings.RecommendationManualHandoffFormat, detail.ManualHandoffReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked),
            string.Format(UiStrings.RecommendationManualActionFormat, detail.ManualActionRequired ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusWait),
            string.Format(UiStrings.RecommendationVerificationFormat, detail.VerificationAvailable ? UiStrings.ActionStatusReturn : UiStrings.ActionStatusWait),
            manualStepGuidance,
            verificationResultSummary,
            nextStep);
    }

    private static VerificationReturnPresentation BuildVerificationReturnPresentation(
        VerificationReturnResult verificationReturn,
        bool manualStepConfirmed)
    {
        var readinessSummary = verificationReturn.IsReady
            ? UiStrings.VerificationReturnReady
            : UiStrings.VerificationReturnNotReady;
        var manualCompletionHint = manualStepConfirmed
            ? UiStrings.VerificationReturnManualConfirmed
            : UiStrings.VerificationReturnManualConfirmHint;

        return new VerificationReturnPresentation(
            UiStrings.VerificationReturnTitle,
            readinessSummary,
            UiStrings.VerificationReturnManualConfirmLabel,
            manualCompletionHint,
            string.Format(UiStrings.VerificationReturnLastSummaryFormat, verificationReturn.LastVerificationSummary),
            verificationReturn.IsReady,
            verificationReturn.ManualCompletionRequired);
    }

    private static string BuildOfficialSourceSummary(OpenOfficialSourceActionResult officialSourceAction)
    {
        if (officialSourceAction.IsReady)
        {
            var url = officialSourceAction.ApprovedOfficialSourceUrl ?? UiStrings.OfficialSourceUrlUnavailable;
            return string.Format(UiStrings.OfficialSourceSummaryReadyFormat, url);
        }

        var reason = string.IsNullOrWhiteSpace(officialSourceAction.BlockReason)
            ? UiStrings.OfficialSourceBlockedNoReason
            : officialSourceAction.BlockReason;
        return string.Format(UiStrings.OfficialSourceSummaryBlockedFormat, reason);
    }

    private static IReadOnlyCollection<UserGuidedActionStepPresentation> BuildUserGuidedSteps(bool hasRecommendation, bool hasReadyHandoff, bool officialSourceReady, bool manualStepConfirmed)
    {
        var reviewStatus = hasRecommendation ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusNotAvailable;
        var sourceStatus = officialSourceReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked;
        var handoffStatus = hasReadyHandoff ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked;
        var manualInstallStatus = hasRecommendation
            ? (manualStepConfirmed ? UiStrings.ActionStatusCompleted : UiStrings.ActionStatusRequired)
            : UiStrings.ActionStatusWait;
        var verificationStatus = hasRecommendation
            ? (manualStepConfirmed ? UiStrings.ActionStatusReadyToVerify : UiStrings.ActionStatusReturn)
            : UiStrings.ActionStatusWait;

        return
        [
            new UserGuidedActionStepPresentation(UiStrings.ActionStepReviewRecommendation, reviewStatus, UiStrings.ActionStepReviewRecommendationHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepOpenOfficialSource, sourceStatus, UiStrings.ActionStepOpenOfficialSourceHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepDownloadManually, handoffStatus, UiStrings.ActionStepDownloadManuallyHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepInstallOutsideApp, manualInstallStatus, UiStrings.ActionStepInstallOutsideAppHint),
            new UserGuidedActionStepPresentation(UiStrings.ActionStepReturnForVerification, verificationStatus, UiStrings.ActionStepReturnForVerificationHint)
        ];
    }
}
