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
    string WorkflowHeadline,
    string WorkflowHint,
    string RecommendationSectionTitle,
    string RecommendationSectionHint,
    string RecommendationEmptyState,
    string ManualSectionTitle,
    string ManualSectionHint,
    string VerificationSectionTitle,
    string VerificationSectionHint,
    string VerificationRescanHint,
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
            UiStrings.WorkflowHeadlineEmpty,
            UiStrings.WorkflowHintEmpty,
            UiStrings.RecommendationSectionTitle,
            UiStrings.RecommendationSectionHint,
            UiStrings.RecommendationEmptyState,
            UiStrings.ManualSectionTitle,
            UiStrings.ManualSectionHint,
            UiStrings.VerificationSectionTitle,
            UiStrings.VerificationSectionHint,
            UiStrings.VerificationRescanHint,
            Array.Empty<RecommendationDetailPresentation>(),
            Array.Empty<UserGuidedActionStepPresentation>());

    public static ScanResultsPresentation FromResult(MainScreenWorkflowResult result)
    {
        var hasRecommendation = result.RecommendedCount > 0;
        var hasReadyHandoff = result.ManualHandoffReadyCount > 0;
        var officialSourceReady = result.OfficialSourceAction.IsReady;

        return new ScanResultsPresentation(
            ScanSummary: string.Format(UiStrings.ScanSummaryFormat, result.ScanSessionId, result.UiCulture, result.ProviderCount),
            DiscoveryAndInspectionSummary: string.Format(UiStrings.DiscoveryInspectionSummaryFormat, result.DiscoveredDeviceCount, result.InspectedDriverCount),
            RecommendationSummary: string.Format(UiStrings.RecommendationSummaryFormat, result.RecommendedCount, result.NotRecommendedCount),
            ManualHandoffSummary: string.Format(UiStrings.ManualHandoffSummaryFormat, result.ManualHandoffReadyCount, result.ManualHandoffUserActionCount),
            VerificationSummary: string.Format(UiStrings.VerificationSummaryFormat, result.VerificationSummary),
            OfficialSourceSummary: BuildOfficialSourceSummary(result.OfficialSourceAction),
            ActionFlowTitle: UiStrings.ActionFlowTitle,
            SafetyNotice: UiStrings.ActionFlowSafetyNotice,
            WorkflowHeadline: BuildWorkflowHeadline(hasRecommendation, officialSourceReady, hasReadyHandoff),
            WorkflowHint: BuildWorkflowHint(hasRecommendation, officialSourceReady, hasReadyHandoff),
            RecommendationSectionTitle: UiStrings.RecommendationSectionTitle,
            RecommendationSectionHint: UiStrings.RecommendationSectionHint,
            RecommendationEmptyState: UiStrings.RecommendationEmptyState,
            ManualSectionTitle: UiStrings.ManualSectionTitle,
            ManualSectionHint: UiStrings.ManualSectionHint,
            VerificationSectionTitle: UiStrings.VerificationSectionTitle,
            VerificationSectionHint: UiStrings.VerificationSectionHint,
            VerificationRescanHint: hasRecommendation ? UiStrings.VerificationRescanHint : UiStrings.VerificationRescanHintNoAction,
            RecommendationDetails: result.RecommendationDetails.Select(MapDetail).ToArray(),
            UserGuidedSteps: BuildUserGuidedSteps(hasRecommendation, hasReadyHandoff, officialSourceReady));
    }

    private static RecommendationDetailPresentation MapDetail(RecommendationDetailResult detail)
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

        var state = ResolveDetailState(detail);

        return new RecommendationDetailPresentation(
            recommendationTitle,
            state.State,
            state.Hint,
            string.Format(UiStrings.RecommendationDeviceFormat, detail.DeviceId),
            reasonSummary,
            string.Format(UiStrings.RecommendationInstalledDriverFormat, detail.InstalledVersion, detail.InstalledProvider ?? UiStrings.RecommendationProviderUnknown),
            candidateSummary,
            string.Format(UiStrings.RecommendationManualHandoffFormat, detail.ManualHandoffReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked),
            string.Format(UiStrings.RecommendationManualActionFormat, detail.ManualActionRequired ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusWait),
            string.Format(UiStrings.RecommendationVerificationFormat, detail.VerificationAvailable ? UiStrings.ActionStatusReturn : UiStrings.ActionStatusWait),
            string.Format(UiStrings.RecommendationVerificationStatusFormat, detail.VerificationStatus),
            nextStep);
    }

    private static (string State, string Hint) ResolveDetailState(RecommendationDetailResult detail)
    {
        if (!detail.HasRecommendation)
        {
            return (UiStrings.RecommendationStateInsufficientEvidence, UiStrings.RecommendationStateInsufficientEvidenceHint);
        }

        if (detail.ManualHandoffReady)
        {
            return (UiStrings.RecommendationStateReadyForManualAction, UiStrings.RecommendationStateReadyForManualActionHint);
        }

        return (UiStrings.RecommendationStateBlocked, UiStrings.RecommendationStateBlockedHint);
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

    private static string BuildWorkflowHeadline(bool hasRecommendation, bool officialSourceReady, bool hasReadyHandoff)
    {
        if (!hasRecommendation)
        {
            return UiStrings.WorkflowHeadlineNoRecommendation;
        }

        if (officialSourceReady && hasReadyHandoff)
        {
            return UiStrings.WorkflowHeadlineReady;
        }

        if (!officialSourceReady)
        {
            return UiStrings.WorkflowHeadlineOfficialSourceLimited;
        }

        return UiStrings.WorkflowHeadlineManualActionLimited;
    }

    private static string BuildWorkflowHint(bool hasRecommendation, bool officialSourceReady, bool hasReadyHandoff)
    {
        if (!hasRecommendation)
        {
            return UiStrings.WorkflowHintNoRecommendation;
        }

        if (officialSourceReady && hasReadyHandoff)
        {
            return UiStrings.WorkflowHintReady;
        }

        if (!officialSourceReady)
        {
            return UiStrings.WorkflowHintOfficialSourceLimited;
        }

        return UiStrings.WorkflowHintManualActionLimited;
    }

    private static IReadOnlyCollection<UserGuidedActionStepPresentation> BuildUserGuidedSteps(bool hasRecommendation, bool hasReadyHandoff, bool officialSourceReady)
    {
        var reviewStatus = hasRecommendation ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusNotAvailable;
        var sourceStatus = officialSourceReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked;
        var handoffStatus = hasReadyHandoff ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked;
        var manualInstallStatus = hasRecommendation ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusWait;
        var verificationStatus = hasRecommendation ? UiStrings.ActionStatusReturn : UiStrings.ActionStatusWait;

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
