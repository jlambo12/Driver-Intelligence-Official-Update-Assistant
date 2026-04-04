using DriverGuardian.Application.Abstractions;
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
    private readonly OfficialSourceProviderLookupCollector _collector = new();

    public async Task<OfficialSourceResolutionResult> ResolveAsync(
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
            return OfficialSourceResolutionResult.NoRecommendationTarget;
        }

        var collection = await _collector.CollectAsync(_providers, targetDriver, cancellationToken);
        var bestCandidate = OfficialSourcePolicyCandidateSelector.SelectBest(collection.PolicyCandidates);

        return new OfficialSourceResolutionResult(
            bestCandidate,
            collection.Failures,
            HasRecommendationTarget: true);
    }
}

public sealed record OfficialSourceResolutionResult(
    OfficialSourcePolicyCandidate? Candidate,
    IReadOnlyCollection<OfficialSourceProviderFailure> Failures,
    bool HasRecommendationTarget)
{
    public static OfficialSourceResolutionResult NoRecommendationTarget { get; } =
        new(null, Array.Empty<OfficialSourceProviderFailure>(), false);
}

public sealed record OfficialSourcePolicyCandidate(
    string ProviderCode,
    string DriverIdentifier,
    SourceEvidence SourceEvidence,
    Uri SourceEvidencePageUri,
    Uri ApprovedNavigationUri,
    Uri? RawDownloadUri,
    OfficialSourceActionTarget ActionTarget,
    int PolicyScore);

public sealed record OfficialSourceProviderFailure(
    string ProviderCode,
    string Message,
    string? ExceptionType);

public sealed class OfficialSourceActionService(
    OfficialSourceResolutionService sourceResolutionService,
    OpenOfficialSourceActionEvaluator openOfficialSourceActionEvaluator,
    IDiagnosticLogger diagnosticLogger)
{
    public async Task<OpenOfficialSourceActionResult> BuildAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> drivers,
        IReadOnlyCollection<RecommendationSummary> recommendations,
        CancellationToken cancellationToken)
    {
        var resolved = await sourceResolutionService.ResolveAsync(drivers, recommendations, cancellationToken);

        if (!resolved.HasRecommendationTarget)
        {
            return new OpenOfficialSourceActionResult(
                IsReady: false,
                ResolutionOutcome: OfficialSourceResolutionOutcome.InsufficientEvidence,
                ActionTarget: OfficialSourceActionTarget.SourcePage,
                Status: "Нет рекомендаций для перехода к официальному источнику.",
                ApprovedOfficialSourceUrl: null,
                BlockReason: null);
        }

        foreach (var failure in resolved.Failures)
        {
            await diagnosticLogger.LogWarningAsync(
                "scan.official_source.provider_lookup.failed",
                $"Provider={failure.ProviderCode}; reason={failure.Message}; exceptionType={failure.ExceptionType ?? "n/a"}",
                cancellationToken);
        }

        if (resolved.Candidate is null)
        {
            return new OpenOfficialSourceActionResult(
                IsReady: false,
                ResolutionOutcome: OfficialSourceResolutionOutcome.InsufficientEvidence,
                ActionTarget: OfficialSourceActionTarget.SourcePage,
                Status: "Не удалось подтвердить официальный источник по доступным провайдерам.",
                ApprovedOfficialSourceUrl: null,
                BlockReason: resolved.Failures.FirstOrDefault()?.Message);
        }

        var decision = openOfficialSourceActionEvaluator.Evaluate(
            new OpenOfficialSourceActionRequest(
                resolved.Candidate.ProviderCode,
                resolved.Candidate.DriverIdentifier,
                resolved.Candidate.SourceEvidence,
                resolved.Candidate.ApprovedNavigationUri,
                AllowDifferentHostOfficialDownload: false));

        return new OpenOfficialSourceActionResult(
            IsReady: decision.IsAllowed,
            ResolutionOutcome: decision.ResolutionOutcome,
            ActionTarget: resolved.Candidate.ActionTarget,
            Status: BuildStatus(decision, resolved.Candidate),
            ApprovedOfficialSourceUrl: decision.Link?.OfficialSourceUri.ToString(),
            BlockReason: decision.Blockers.FirstOrDefault()?.Reason.ToString());
    }

    private static string BuildStatus(OpenOfficialSourceActionDecision decision, OfficialSourcePolicyCandidate candidate)
    {
        if (!decision.IsAllowed)
        {
            return "Открытие официального источника требует ручной проверки.";
        }

        if (candidate.RawDownloadUri is not null
            && !string.Equals(candidate.RawDownloadUri.Host, candidate.SourceEvidencePageUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return "Подтверждена официальная страница источника; внешняя CDN-ссылка сохранена как дополнительное, но не основное направление перехода.";
        }

        return candidate.ActionTarget switch
        {
            OfficialSourceActionTarget.DirectDownloadPage => "Подтверждена прямая официальная страница драйвера для ручного перехода.",
            _ => "Подтверждена официальная страница источника для ручного перехода."
        };
    }
}
