using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed record ScanResultsPresentation(
    bool HasScanData,
    string ScanSummary,
    string ScanOverviewEmptyState,
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
    string PrimaryRecommendationSummary,
    bool ShowSecondaryRecommendationSummary,
    string SecondaryRecommendationSummary,
    string SecondaryRecommendationToggleLabel,
    string SecondaryRecommendationHideLabel,
    int SecondaryRecommendationCount,
    string ManualSectionTitle,
    string ManualSectionHint,
    string ManualSectionEmptyState,
    string VerificationSectionTitle,
    string VerificationSectionHint,
    string VerificationRescanHint,
    string VerificationEmptyState,
    bool ShowVerificationEmptyState,
    IReadOnlyCollection<RecommendationDetailPresentation> RecommendationDetails,
    IReadOnlyCollection<RecommendationDetailPresentation> SecondaryRecommendationDetails,
    IReadOnlyCollection<UserGuidedActionStepPresentation> UserGuidedSteps)
{
    private const int MaxPrimaryRecommendationEntries = 6;

    public static ScanResultsPresentation Empty() =>
        new(
            false,
            string.Empty,
            UiStrings.ScanOverviewEmptyState,
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
            UiStrings.RecommendationEmptyStatePreScan,
            string.Empty,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            UiStrings.ManualSectionTitle,
            UiStrings.ManualSectionHint,
            UiStrings.ManualSectionEmptyState,
            UiStrings.VerificationSectionTitle,
            UiStrings.VerificationSectionHint,
            UiStrings.VerificationRescanHintNoAction,
            UiStrings.VerificationSectionEmptyStatePreScan,
            true,
            Array.Empty<RecommendationDetailPresentation>(),
            Array.Empty<RecommendationDetailPresentation>(),
            Array.Empty<UserGuidedActionStepPresentation>());

    public static ScanResultsPresentation FromResult(MainScreenWorkflowResult result)
    {
        var hasRecommendation = result.RecommendedCount > 0;
        var hasReadyHandoff = result.ManualHandoffReadyCount > 0;
        var officialSourceReady = result.OfficialSourceAction.IsReady;

        var prioritizedDetails = PrioritizeAndFilterDetails(result.RecommendationDetails);
        var secondaryDetails = result.RecommendationDetails
            .Except(prioritizedDetails)
            .OrderBy(detail => HumanizeDeviceLabel(detail.DeviceDisplayName), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var hiddenCount = secondaryDetails.Length;

        return new ScanResultsPresentation(
            HasScanData: true,
            ScanSummary: string.Format(UiStrings.ScanSummaryFormat, result.DiscoveredDeviceCount, result.InspectedDriverCount),
            ScanOverviewEmptyState: string.Empty,
            DiscoveryAndInspectionSummary: string.Format(UiStrings.DiscoveryInspectionSummaryFormat, result.DiscoveredDeviceCount, result.InspectedDriverCount),
            RecommendationSummary: string.Format(UiStrings.RecommendationSummaryFormat, result.RecommendedCount + result.NotRecommendedCount, result.ManualHandoffUserActionCount, result.RecommendedCount),
            ManualHandoffSummary: string.Format(UiStrings.ManualHandoffSummaryFormat, result.ManualHandoffReadyCount, officialSourceReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked),
            VerificationSummary: string.Format(UiStrings.VerificationSummaryFormat, result.VerificationSummary),
            OfficialSourceSummary: BuildOfficialSourceSummary(result.OfficialSourceAction),
            ActionFlowTitle: UiStrings.ActionFlowTitle,
            SafetyNotice: UiStrings.ActionFlowSafetyNotice,
            WorkflowHeadline: BuildWorkflowHeadline(hasRecommendation, officialSourceReady, hasReadyHandoff),
            WorkflowHint: BuildWorkflowHint(hasRecommendation, officialSourceReady, hasReadyHandoff),
            RecommendationSectionTitle: UiStrings.RecommendationSectionTitle,
            RecommendationSectionHint: UiStrings.RecommendationSectionHint,
            RecommendationEmptyState: hasRecommendation ? UiStrings.RecommendationEmptyState : UiStrings.RecommendationEmptyStateNoAction,
            PrimaryRecommendationSummary: string.Format(UiStrings.RecommendationPrimarySummaryFormat, prioritizedDetails.Count, result.RecommendationDetails.Count),
            ShowSecondaryRecommendationSummary: hiddenCount > 0,
            SecondaryRecommendationSummary: hiddenCount > 0
                ? string.Format(UiStrings.RecommendationSecondarySummaryFormat, hiddenCount)
                : string.Empty,
            SecondaryRecommendationToggleLabel: hiddenCount > 0
                ? string.Format(UiStrings.RecommendationSecondaryToggleFormat, hiddenCount)
                : string.Empty,
            SecondaryRecommendationHideLabel: UiStrings.RecommendationSecondaryHide,
            SecondaryRecommendationCount: hiddenCount,
            RecommendationDetails: prioritizedDetails.Select(MapDetail).ToArray(),
            SecondaryRecommendationDetails: secondaryDetails.Select(MapDetail).ToArray(),
            ManualSectionTitle: UiStrings.ManualSectionTitle,
            ManualSectionHint: UiStrings.ManualSectionHint,
            ManualSectionEmptyState: UiStrings.ManualSectionEmptyState,
            VerificationSectionTitle: UiStrings.VerificationSectionTitle,
            VerificationSectionHint: UiStrings.VerificationSectionHint,
            VerificationRescanHint: hasRecommendation ? UiStrings.VerificationRescanHint : UiStrings.VerificationRescanHintNoAction,
            VerificationEmptyState: hasRecommendation ? string.Empty : UiStrings.VerificationSectionEmptyStateNoAction,
            ShowVerificationEmptyState: !hasRecommendation,
            UserGuidedSteps: hasRecommendation
                ? BuildUserGuidedSteps(hasRecommendation, hasReadyHandoff, officialSourceReady)
                : Array.Empty<UserGuidedActionStepPresentation>());
    }

    private static IReadOnlyCollection<RecommendationDetailResult> PrioritizeAndFilterDetails(IReadOnlyCollection<RecommendationDetailResult> details)
    {
        return details
            .OrderBy(ResolvePriorityBucket)
            .ThenByDescending(detail => detail.HasRecommendation)
            .ThenBy(detail => HumanizeDeviceLabel(detail.DeviceDisplayName), StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxPrimaryRecommendationEntries)
            .ToArray();
    }

    private static int ResolvePriorityBucket(RecommendationDetailResult detail)
    {
        return detail.PriorityBucket;
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

        var deviceTitle = BuildDeviceTitle(detail);
        var technicalSummary = BuildTechnicalIdentifierSummary(detail.DeviceId);

        var verificationStateLabel = ResolveVerificationStateLabel(detail);

        return new RecommendationDetailPresentation(
            recommendationTitle,
            state.State,
            state.Hint,
            string.Join(Environment.NewLine, [deviceTitle, technicalSummary]),
            reasonSummary,
            string.Format(UiStrings.RecommendationInstalledDriverFormat, detail.InstalledVersion, detail.InstalledProvider ?? UiStrings.RecommendationProviderUnknown),
            candidateSummary,
            string.Format(UiStrings.RecommendationManualHandoffFormat, detail.ManualHandoffReady ? UiStrings.ActionStatusAvailable : UiStrings.ActionStatusBlocked),
            string.Format(UiStrings.RecommendationManualActionFormat, detail.ManualActionRequired ? UiStrings.ActionStatusRequired : UiStrings.ActionStatusWait),
            string.Format(UiStrings.RecommendationVerificationFormat, verificationStateLabel),
            string.Format(UiStrings.RecommendationVerificationStatusFormat, detail.VerificationStatus),
            nextStep);
    }

    private static string BuildDeviceTitle(RecommendationDetailResult detail)
    {
        var preferredLabel = string.IsNullOrWhiteSpace(detail.DeviceDisplayName)
            ? detail.DeviceId
            : detail.DeviceDisplayName;
        var cleaned = HumanizeDeviceLabel(preferredLabel);

        return string.Format(UiStrings.RecommendationDeviceFormat, cleaned);
    }

    private static string BuildTechnicalIdentifierSummary(string deviceId)
    {
        return string.Format(UiStrings.RecommendationTechnicalIdFormat, deviceId);
    }

    private static string HumanizeDeviceLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return UiStrings.RecommendationDeviceUnknown;
        }

        var trimmed = label.Trim();
        if (LooksTechnical(trimmed))
        {
            return UiStrings.RecommendationDeviceGeneric;
        }

        return trimmed;
    }

    private static bool LooksTechnical(string value)
    {
        return value.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("VID_", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("VEN_", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("{", StringComparison.OrdinalIgnoreCase);
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

    private static string ResolveVerificationStateLabel(RecommendationDetailResult detail)
    {
        if (!detail.VerificationAvailable)
        {
            return UiStrings.ActionStatusNoActionNeeded;
        }

        return detail.ManualHandoffReady
            ? UiStrings.ActionStatusVerificationExpected
            : UiStrings.ActionStatusWaitingForReturn;
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
