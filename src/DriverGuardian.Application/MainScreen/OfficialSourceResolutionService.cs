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

        var failures = new List<OfficialSourceProviderFailure>();
        var policyCandidates = new List<OfficialSourcePolicyCandidate>();

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProviderLookupResponse response;
            try
            {
                response = await provider.LookupAsync(new ProviderLookupRequest(
                        provider.Descriptor.Code,
                        targetDriver.DeviceIdentity.InstanceId,
                        [targetDriver.HardwareIdentifier.Value],
                        targetDriver.DriverVersion,
                        OperatingSystemVersion: null,
                        DeviceManufacturer: targetDriver.ProviderName,
                        DeviceModel: null),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                failures.Add(new OfficialSourceProviderFailure(provider.Descriptor.Code, ex.Message, ex.GetType().Name));
                continue;
            }

            if (!response.IsSuccess)
            {
                failures.Add(new OfficialSourceProviderFailure(provider.Descriptor.Code, response.FailureReason ?? "Unknown provider failure.", null));
                continue;
            }

            foreach (var candidate in response.Candidates)
            {
                if (!TryBuildPolicyCandidate(provider.Descriptor.Code, candidate, out var policyCandidate))
                {
                    continue;
                }

                policyCandidates.Add(policyCandidate);
            }
        }

        var bestCandidate = policyCandidates
            .OrderByDescending(candidate => candidate.PolicyScore)
            .ThenBy(candidate => candidate.ProviderCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return new OfficialSourceResolutionResult(
            bestCandidate,
            failures,
            HasRecommendationTarget: true);
    }

    // Safety-first policy:
    // 1) source evidence page is the trust anchor.
    // 2) approved navigation target is never replaced by an external CDN download URI.
    // 3) direct official driver page is approved only for strong official publisher trust.
    // 4) vendor support / catalog trust resolves to source page navigation.
    private static bool TryBuildPolicyCandidate(
        string providerCode,
        ProviderCandidate candidate,
        out OfficialSourcePolicyCandidate policyCandidate)
    {
        var sourceEvidence = candidate.SourceEvidence;
        if (!sourceEvidence.IsOfficialSource || sourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            policyCandidate = null!;
            return false;
        }

        if (!string.Equals(sourceEvidence.SourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            policyCandidate = null!;
            return false;
        }

        var actionTarget = sourceEvidence.TrustLevel == SourceTrustLevel.OfficialPublisherSite
            ? OfficialSourceActionTarget.DirectDownloadPage
            : OfficialSourceActionTarget.SourcePage;

        var approvedNavigationUri = sourceEvidence.SourceUri;

        policyCandidate = new OfficialSourcePolicyCandidate(
            providerCode,
            candidate.DriverIdentifier,
            sourceEvidence,
            SourceEvidencePageUri: sourceEvidence.SourceUri,
            ApprovedNavigationUri: approvedNavigationUri,
            RawDownloadUri: candidate.DownloadUri,
            actionTarget,
            CalculatePolicyScore(sourceEvidence.TrustLevel, candidate.CompatibilityConfidence));

        return true;
    }

    private static int CalculatePolicyScore(SourceTrustLevel trustLevel, CompatibilityConfidence compatibility)
    {
        var trustScore = trustLevel switch
        {
            SourceTrustLevel.OfficialPublisherSite => 300,
            SourceTrustLevel.OemSupportPortal => 200,
            SourceTrustLevel.OperatingSystemCatalog => 150,
            _ => 0
        };

        var compatibilityScore = compatibility switch
        {
            CompatibilityConfidence.High => 40,
            CompatibilityConfidence.Medium => 25,
            CompatibilityConfidence.Low => 10,
            _ => 0
        };

        return trustScore + compatibilityScore;
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
