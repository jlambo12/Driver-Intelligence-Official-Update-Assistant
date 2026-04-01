using DriverGuardian.Domain.Enums;
using DriverGuardian.Domain.ValueObjects;
using DriverGuardian.ProviderAdapters.Abstractions.Contracts;
using DriverGuardian.ProviderAdapters.Abstractions.Models;

namespace DriverGuardian.ProviderAdapters.Official.Services;

public sealed class OfficialProviderStubAdapter : IOfficialDriverProviderAdapter
{
    public string ProviderId => "official.stub";

    public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
    {
        var evidence = new SourceEvidence(
            DriverSourceProvenance.Unsupported,
            new Uri("https://example.invalid/official-source-not-configured"),
            "STUB",
            DateTimeOffset.UtcNow,
            false);

        var compatibility = new CompatibilityAssessmentResult(
            new CompatibilityConfidence(CompatibilityConfidenceLevel.Ambiguous, 0.20m),
            RequiresManualVerification: true,
            ReasonCode: "NO_OFFICIAL_PROVIDER_CONFIGURED");

        return Task.FromResult(new ProviderLookupResponse(
            RecommendedVersion: null,
            Compatibility: compatibility,
            Evidence: [evidence],
            IsAmbiguous: true,
            SummaryCode: "ANALYSIS_ONLY_STUB"));
    }
}
