using DriverGuardian.Application.Presentation;
using DriverGuardian.Contracts.DeviceDiscovery;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Application.MainScreen;

public sealed class RecommendationDetailAssembler
{
    public IReadOnlyCollection<RecommendationDetailResult> Assemble(
        IReadOnlyCollection<DiscoveredDevice> discoveredDevices,
        IReadOnlyCollection<InstalledDriverSnapshot> drivers,
        IReadOnlyCollection<RecommendationSummary> recommendations)
    {
        var byDevice = recommendations.ToDictionary(item => item.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);
        var discoveredById = discoveredDevices.ToDictionary(
            device => device.Identity.InstanceId,
            device => device,
            StringComparer.OrdinalIgnoreCase);

        return drivers.Select(driver =>
            {
                var hasRecommendation = byDevice.TryGetValue(driver.DeviceIdentity.InstanceId, out var recommendation) && recommendation.HasRecommendation;
                discoveredById.TryGetValue(driver.DeviceIdentity.InstanceId, out var discoveredDevice);
                var displayName = DevicePresentationHeuristics.BuildUserFacingName(discoveredDevice, driver.DeviceIdentity.InstanceId);

                return new RecommendationDetailResult(
                    DeviceDisplayName: displayName,
                    DeviceId: driver.DeviceIdentity.InstanceId,
                    PriorityBucket: DevicePresentationHeuristics.ResolvePriorityBucket(discoveredDevice, hasRecommendation),
                    HasRecommendation: hasRecommendation,
                    RecommendationReason: recommendation?.Reason ?? string.Empty,
                    InstalledVersion: driver.DriverVersion,
                    InstalledProvider: driver.ProviderName,
                    RecommendedVersion: recommendation?.RecommendedVersion,
                    OfficialSourceUrl: recommendation?.OfficialSourceUrl,
                    ManualHandoffReady: false,
                    ManualActionRequired: hasRecommendation,
                    VerificationAvailable: hasRecommendation,
                    VerificationStatus: hasRecommendation
                        ? "Ожидается ваш возврат: после ручной установки выполните повторный анализ для проверки результата."
                        : "Действие не требуется: возврат для проверки по этому устройству не ожидается.",
                    RecommendationReasonCode: MapRecommendationReasonCode(recommendation));
            })
            .Where(detail =>
            {
                if (detail.HasRecommendation)
                {
                    return true;
                }

                if (!discoveredById.TryGetValue(detail.DeviceId, out var device))
                {
                    return true;
                }

                return DevicePresentationHeuristics.IsUserRelevant(device, detail.HasRecommendation);
            })
            .OrderBy(detail => detail.PriorityBucket)
            .ThenBy(detail => detail.DeviceDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static RecommendationDetailReasonCode MapRecommendationReasonCode(RecommendationSummary? recommendation)
    {
        if (recommendation is null)
        {
            return RecommendationDetailReasonCode.Unknown;
        }

        return recommendation.ReasonCode switch
        {
            RecommendationSummaryReasonCode.RecommendedUpgradeAvailable => RecommendationDetailReasonCode.RecommendedUpgradeAvailable,
            RecommendationSummaryReasonCode.AlreadyUpToDate => RecommendationDetailReasonCode.AlreadyUpToDate,
            RecommendationSummaryReasonCode.CandidateMarkedIncompatible => RecommendationDetailReasonCode.CandidateMarkedIncompatible,
            RecommendationSummaryReasonCode.CandidateCompatibilityUnknown => RecommendationDetailReasonCode.CandidateCompatibilityUnknown,
            RecommendationSummaryReasonCode.InsufficientEvidence => RecommendationDetailReasonCode.InsufficientEvidence,
            RecommendationSummaryReasonCode.InsufficientEvidenceDueToProviderFailures => RecommendationDetailReasonCode.InsufficientEvidenceDueToProviderFailures,
            _ => RecommendationDetailReasonCode.Unknown
        };
    }
}
