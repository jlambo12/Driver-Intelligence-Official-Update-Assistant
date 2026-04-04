using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed partial record ScanResultsPresentation
{
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
        var verificationStatus = hasRecommendation
            ? (hasReadyHandoff ? UiStrings.ActionStatusVerificationExpected : UiStrings.ActionStatusWaitingForReturn)
            : UiStrings.ActionStatusNoActionNeeded;

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
