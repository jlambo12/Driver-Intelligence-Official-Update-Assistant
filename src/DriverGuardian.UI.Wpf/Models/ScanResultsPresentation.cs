using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed partial record ScanResultsPresentation(
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
    string OpenOfficialSourceActionLabel,
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
            UiStrings.OfficialSourceOpenActionLabel,
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
            OpenOfficialSourceActionLabel: UiStrings.OfficialSourceOpenActionLabel,
            VerificationSectionTitle: UiStrings.VerificationSectionTitle,
            VerificationSectionHint: UiStrings.VerificationSectionHint,
            VerificationRescanHint: hasRecommendation ? UiStrings.VerificationRescanHint : UiStrings.VerificationRescanHintNoAction,
            VerificationEmptyState: hasRecommendation ? string.Empty : UiStrings.VerificationSectionEmptyStateNoAction,
            ShowVerificationEmptyState: !hasRecommendation,
            UserGuidedSteps: hasRecommendation
                ? BuildUserGuidedSteps(hasRecommendation, hasReadyHandoff, officialSourceReady)
                : Array.Empty<UserGuidedActionStepPresentation>());
    }
}
