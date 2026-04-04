using DriverGuardian.Application.Abstractions;
using DriverGuardian.UI.Wpf.Localization;

namespace DriverGuardian.UI.Wpf.Models;

public sealed partial record ScanResultsPresentation
{
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
}
