using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Application.MainScreen;

public sealed class OfficialSourceResolutionService(IEnumerable<IOfficialProviderAdapter> providers)
{
    private static readonly string[] AllowedDifferentHostCdnSuffixes =
    [
        ".microsoft.com",
        ".windowsupdate.com",
        ".download.windowsupdate.com"
    ];

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
                if (!candidate.SourceEvidence.IsOfficialSource || candidate.SourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
                {
                    continue;
                }

                policyCandidates.Add(BuildPolicyCandidate(provider.Descriptor.Code, candidate));
            }
        }

        var bestCandidate = policyCandidates
            .OrderByDescending(candidate => candidate.PolicyScore)
            .ThenBy(candidate => candidate.ProviderCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return new OfficialSourceResolutionResult(
            bestCandidate,
            failures,
            hasRecommendationTarget: true);
    }

    // Policy:
    // - SourcePage: when direct link is absent, non-HTTPS, or cross-host download is not explicitly allowed.
    // - DirectDownloadPage: when download URI is HTTPS and host either matches source host or is allowlisted CDN.
    // - Different-host CDN is allowed only for trusted official evidence and known CDN suffixes.
    private static OfficialSourcePolicyCandidate BuildPolicyCandidate(string providerCode, ProviderCandidate candidate)
    {
        var actionTarget = ResolveActionTarget(candidate);
        var targetUri = actionTarget == OfficialSourceActionTarget.DirectDownloadPage
            ? candidate.DownloadUri!
            : candidate.SourceEvidence.SourceUri;

        return new OfficialSourcePolicyCandidate(
            providerCode,
            candidate.DriverIdentifier,
            candidate.SourceEvidence,
            targetUri,
            actionTarget,
            CalculatePolicyScore(candidate, actionTarget));
    }

    private static OfficialSourceActionTarget ResolveActionTarget(ProviderCandidate candidate)
    {
        if (candidate.DownloadUri is null)
        {
            return OfficialSourceActionTarget.SourcePage;
        }

        if (!string.Equals(candidate.DownloadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return OfficialSourceActionTarget.SourcePage;
        }

        if (string.Equals(candidate.DownloadUri.Host, candidate.SourceEvidence.SourceUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return OfficialSourceActionTarget.DirectDownloadPage;
        }

        return IsAllowedDifferentHostCdn(candidate.SourceEvidence, candidate.DownloadUri)
            ? OfficialSourceActionTarget.DirectDownloadPage
            : OfficialSourceActionTarget.SourcePage;
    }

    private static bool IsAllowedDifferentHostCdn(SourceEvidence sourceEvidence, Uri downloadUri)
    {
        if (!sourceEvidence.IsOfficialSource || sourceEvidence.TrustLevel == SourceTrustLevel.Unknown)
        {
            return false;
        }

        var host = downloadUri.Host;
        return AllowedDifferentHostCdnSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static int CalculatePolicyScore(ProviderCandidate candidate, OfficialSourceActionTarget actionTarget)
    {
        var trustScore = candidate.SourceEvidence.TrustLevel switch
        {
            SourceTrustLevel.OfficialPublisherSite => 400,
            SourceTrustLevel.OemSupportPortal => 300,
            SourceTrustLevel.OperatingSystemCatalog => 200,
            _ => 0
        };

        var compatibilityScore = candidate.CompatibilityConfidence switch
        {
            CompatibilityConfidence.High => 40,
            CompatibilityConfidence.Medium => 25,
            CompatibilityConfidence.Low => 10,
            _ => 0
        };

        var targetScore = actionTarget == OfficialSourceActionTarget.DirectDownloadPage ? 15 : 5;
        return trustScore + compatibilityScore + targetScore;
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
    Uri ActionUri,
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
                resolved.Candidate.ActionUri,
                AllowDifferentHostOfficialDownload:
                resolved.Candidate.ActionTarget == OfficialSourceActionTarget.DirectDownloadPage
                && !string.Equals(
                    resolved.Candidate.ActionUri.Host,
                    resolved.Candidate.SourceEvidence.SourceUri.Host,
                    StringComparison.OrdinalIgnoreCase)));

        return new OpenOfficialSourceActionResult(
            IsReady: decision.IsAllowed,
            ResolutionOutcome: decision.ResolutionOutcome,
            ActionTarget: resolved.Candidate.ActionTarget,
            Status: BuildStatus(decision, resolved.Candidate.ActionTarget),
            ApprovedOfficialSourceUrl: decision.Link?.OfficialSourceUri.ToString(),
            BlockReason: decision.Blockers.FirstOrDefault()?.Reason.ToString());
    }

    private static string BuildStatus(OpenOfficialSourceActionDecision decision, OfficialSourceActionTarget actionTarget)
    {
        if (!decision.IsAllowed)
        {
            return "Открытие официального источника требует ручной проверки.";
        }

        return actionTarget switch
        {
            OfficialSourceActionTarget.DirectDownloadPage => "Подтверждена прямая страница загрузки из официального источника.",
            _ when decision.ResolutionOutcome == OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage =>
                "Подтверждена прямая официальная страница драйвера для ручного перехода.",
            _ => "Подтверждена официальная страница источника для ручного перехода."
        };
    }
}
