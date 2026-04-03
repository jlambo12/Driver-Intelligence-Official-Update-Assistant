using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Application.MainScreen;

public sealed class OfficialSourceResolutionService(IEnumerable<IOfficialProviderAdapter> providers)
{
    private readonly IReadOnlyCollection<IOfficialProviderAdapter> _providers = providers
        .Where(provider => provider.Descriptor.IsEnabled)
        .ToArray();

    public async Task<OfficialSourceResolutionResult?> ResolveAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> drivers,
        IReadOnlyCollection<RecommendationSummary> recommendations,
        CancellationToken cancellationToken)
    {
        var recommendedDevices = recommendations
            .Where(item => item.HasRecommendation)
            .Select(item => item.DeviceIdentity.InstanceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetDriver = drivers.FirstOrDefault(driver => recommendedDevices.Contains(driver.DeviceIdentity.InstanceId));
        if (targetDriver is null)
        {
            return null;
        }

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await provider.LookupAsync(new ProviderLookupRequest(
                    provider.Descriptor.Code,
                    targetDriver.DeviceIdentity.InstanceId,
                    [targetDriver.HardwareIdentifier.Value],
                    targetDriver.DriverVersion,
                    OperatingSystemVersion: null,
                    DeviceManufacturer: targetDriver.ProviderName,
                    DeviceModel: null),
                cancellationToken);

            if (!response.IsSuccess)
            {
                continue;
            }

            var candidate = response.Candidates.FirstOrDefault(item =>
                item.SourceEvidence.IsOfficialSource
                && item.SourceEvidence.TrustLevel != SourceTrustLevel.Unknown);

            if (candidate is null)
            {
                continue;
            }

            var officialUri = candidate.DownloadUri ?? candidate.SourceEvidence.SourceUri;

            return new OfficialSourceResolutionResult(
                provider.Descriptor.Code,
                candidate.DriverIdentifier,
                candidate.SourceEvidence,
                officialUri);
        }

        return null;
    }
}

public sealed record OfficialSourceResolutionResult(
    string ProviderCode,
    string DriverIdentifier,
    SourceEvidence SourceEvidence,
    Uri OfficialSourceUri);

public sealed class OfficialSourceActionService(
    OfficialSourceResolutionService sourceResolutionService,
    OpenOfficialSourceActionEvaluator openOfficialSourceActionEvaluator)
{
    public async Task<OpenOfficialSourceActionResult> BuildAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> drivers,
        IReadOnlyCollection<RecommendationSummary> recommendations,
        CancellationToken cancellationToken)
    {
        var resolved = await sourceResolutionService.ResolveAsync(drivers, recommendations, cancellationToken);
        if (resolved is null)
        {
            return new OpenOfficialSourceActionResult(
                IsReady: false,
                ResolutionOutcome: OfficialSourceResolutionOutcome.InsufficientEvidence,
                Status: "Нет рекомендаций для перехода к официальному источнику.",
                ApprovedOfficialSourceUrl: null,
                BlockReason: null);
        }

        var decision = openOfficialSourceActionEvaluator.Evaluate(
            new OpenOfficialSourceActionRequest(
                resolved.ProviderCode,
                resolved.DriverIdentifier,
                resolved.SourceEvidence,
                resolved.OfficialSourceUri));

        return new OpenOfficialSourceActionResult(
            IsReady: decision.IsAllowed,
            ResolutionOutcome: decision.ResolutionOutcome,
            Status: BuildStatus(decision),
            ApprovedOfficialSourceUrl: decision.Link?.OfficialSourceUri.ToString(),
            BlockReason: decision.Blockers.FirstOrDefault()?.Reason.ToString());
    }

    private static string BuildStatus(OpenOfficialSourceActionDecision decision)
    {
        if (!decision.IsAllowed)
        {
            return "Открытие официального источника требует ручной проверки.";
        }

        return decision.ResolutionOutcome switch
        {
            OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage =>
                "Подтверждена прямая официальная страница драйвера для ручного перехода.",
            OfficialSourceResolutionOutcome.ConfirmedVendorSupportPage =>
                "Подтверждена официальная страница поддержки производителя для ручного перехода.",
            _ => "Официальный источник требует ручной проверки."
        };
    }
}
