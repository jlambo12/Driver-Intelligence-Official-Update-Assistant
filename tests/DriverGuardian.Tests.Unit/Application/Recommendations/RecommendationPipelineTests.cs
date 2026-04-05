using DriverGuardian.Application.Recommendations;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;
using DriverGuardian.ProviderAdapters.Official.Registry;
using RecommendationProviderPrecedence = DriverGuardian.Application.Recommendations.ProviderPrecedence;

namespace DriverGuardian.Tests.Unit.Application.Recommendations;

public sealed class RecommendationPipelineTests
{
    [Fact]
    public async Task BuildAsync_ShouldReturnInsufficientEvidence_WhenNoProviderCandidates()
    {
        var pipeline = new RecommendationPipeline([new TestProviderAdapter(CreateSuccessResponse("official", []))]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.False(summary.HasRecommendation);
        Assert.Null(summary.RecommendedVersion);
        Assert.Contains("insufficient evidence", summary.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DriverGuardian.Domain.Recommendations.RecommendationSummaryReasonCode.InsufficientEvidence, summary.ReasonCode);
    }

    [Fact]
    public async Task BuildAsync_ShouldRecommend_WhenNewerCompatibleOfficialCandidateExists()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.True(summary.HasRecommendation);
        Assert.Equal("2.0.0", summary.RecommendedVersion);
        Assert.Equal("https://example.test/driver", summary.OfficialSourceUrl);
        Assert.Equal(DriverGuardian.Domain.Recommendations.RecommendationSummaryReasonCode.RecommendedUpgradeAvailable, summary.ReasonCode);
    }

    [Fact]
    public async Task BuildAsync_ShouldNotRecommend_WhenCandidateIsNotNewer()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("1.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.False(summary.HasRecommendation);
        Assert.Contains("up to date", summary.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DriverGuardian.Domain.Recommendations.RecommendationSummaryReasonCode.AlreadyUpToDate, summary.ReasonCode);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnHonestNonRecommendation_WhenLookupFails()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(new ProviderLookupResponse("official", false, [], "service unavailable"))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.False(summary.HasRecommendation);
        Assert.Contains("lookup failed", summary.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("service unavailable", summary.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DriverGuardian.Domain.Recommendations.RecommendationSummaryReasonCode.InsufficientEvidenceDueToProviderFailures, summary.ReasonCode);
    }

    [Fact]
    public async Task BuildAsync_ShouldUseEvaluatorWinner_WhenMultipleCandidatesAreReturned()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)])),
            new TestProviderAdapter(CreateSuccessResponse("oem", [CreateCandidate("2.2.0", CompatibilityConfidence.High, false, SourceTrustLevel.OemSupportPortal)]), "oem")
        ], providerPrecedence: RecommendationProviderPrecedence.OemFirst);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.True(summary.HasRecommendation);
        Assert.Equal("2.2.0", summary.RecommendedVersion);
        Assert.Null(summary.OfficialSourceUrl);
    }


    [Fact]
    public async Task BuildAsync_ShouldUseEnabledProviders_WhenDisabledProvidersArePresent()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(
                CreateSuccessResponse("disabled", [CreateCandidate("9.9.9", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]),
                code: "disabled",
                isEnabled: false),
            new TestProviderAdapter(
                CreateSuccessResponse("official", [CreateCandidate("2.1.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]),
                code: "official",
                isEnabled: true)
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.True(summary.HasRecommendation);
        Assert.Equal("2.1.0", summary.RecommendedVersion);
    }

    [Fact]
    public async Task BuildAsync_ShouldProcessMultipleInstalledDriversIndependently()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(
                new Dictionary<string, ProviderLookupResponse>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DEV-1"] = CreateSuccessResponse("official", [CreateCandidate("2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]),
                    ["DEV-2"] = CreateSuccessResponse("official", [CreateCandidate("1.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)])
                })
        ]);

        var result = await pipeline.BuildAsync(
            [
                CreateInstalled("DEV-1", "1.0.0"),
                CreateInstalled("DEV-2", "1.0.0")
            ],
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(x => x.DeviceIdentity.InstanceId == "DEV-1").HasRecommendation);
        Assert.False(result.Single(x => x.DeviceIdentity.InstanceId == "DEV-2").HasRecommendation);
    }

    [Fact]
    public async Task BuildAsync_ShouldUseRealWindowsCatalogProvider_InSupportedRuntimeScenario()
    {
        var pipeline = new RecommendationPipeline([new OfficialWindowsCatalogProviderAdapter()]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "31.0.101.2000", "PCI\\VEN_8086&DEV_15F3")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.False(summary.HasRecommendation);
        Assert.Null(summary.RecommendedVersion);
        Assert.Contains("strict recommendation threshold", summary.Reason, StringComparison.OrdinalIgnoreCase);
    }



    [Fact]
    public async Task BuildAsync_ShouldSkipLowValueTechnicalDriversForDeepLookup()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("99.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("SWD\\MMDEVAPI\\{FAKE}", "1.0.0", "ROOT\\MMDEVAPI")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.False(summary.HasRecommendation);
        Assert.Contains("skipped for deep provider lookup", summary.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DriverGuardian.Domain.Recommendations.RecommendationSummaryReasonCode.InsufficientEvidence, summary.ReasonCode);
    }

    [Fact]
    public async Task BuildAsync_ShouldNotSkipUsefulUsbAudioEndpoint()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite)]))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("SWD\\MMDEVAPI\\{GUID}", "1.0.0", "USB\\VID_046D&PID_0A87")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.True(summary.HasRecommendation);
        Assert.DoesNotContain("skipped for deep provider lookup", summary.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_ShouldNotExposeOfficialSourceUrl_WhenSourceUriIsNotSafeHttps()
    {
        var pipeline = new RecommendationPipeline([
            new TestProviderAdapter(CreateSuccessResponse("official", [CreateCandidate("2.0.0", CompatibilityConfidence.High, true, SourceTrustLevel.OfficialPublisherSite, sourceUri: new Uri("http://example.test/driver"))]))
        ]);

        var result = await pipeline.BuildAsync([CreateInstalled("DEV-1", "1.0.0")], CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.True(summary.HasRecommendation);
        Assert.Null(summary.OfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnEmpty_WhenNoInstalledDriversProvided()
    {
        var pipeline = new RecommendationPipeline([new TestProviderAdapter(CreateSuccessResponse("official", []))]);

        var result = await pipeline.BuildAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    private static InstalledDriverSnapshot CreateInstalled(string deviceId, string version, string? hardwareId = null)
        => new(
            new DeviceIdentity(deviceId),
            new HardwareIdentifier(hardwareId ?? $"PCI\\VEN_1234&DEV_{deviceId}"),
            version,
            null,
            "Contoso");

    private static ProviderLookupResponse CreateSuccessResponse(string providerCode, IReadOnlyCollection<ProviderCandidate> candidates)
        => new(providerCode, true, candidates, null);

    private static ProviderCandidate CreateCandidate(
        string version,
        CompatibilityConfidence confidence,
        bool isOfficial,
        SourceTrustLevel trustLevel,
        Uri? sourceUri = null)
        => new(
            DriverIdentifier: "DRV-1",
            CandidateVersion: version,
            ReleaseDateIso: null,
            CompatibilityConfidence: confidence,
            MatchStrength: HardwareIdMatchStrength.ExactHardwareId,
            ConfidenceRationale: "test candidate",
            SourceEvidence: new SourceEvidence(
                sourceUri ?? new Uri("https://example.test/driver"),
                "Example Publisher",
                trustLevel,
                isOfficial,
                "pipeline-test"),
            DownloadUri: new Uri("https://example.test/download"));

    private sealed class TestProviderAdapter : IOfficialProviderAdapter
    {
        private readonly Func<ProviderLookupRequest, ProviderLookupResponse> _lookup;

        public TestProviderAdapter(ProviderLookupResponse response, string code = "official", bool isEnabled = true)
            : this(_ => response, code, isEnabled)
        {
        }

        public TestProviderAdapter(IReadOnlyDictionary<string, ProviderLookupResponse> responsesByDevice, string code = "official", bool isEnabled = true)
            : this(request => responsesByDevice.TryGetValue(request.DeviceInstanceId, out var response)
                ? response
                : new ProviderLookupResponse(code, true, [], null), code, isEnabled)
        {
        }

        private TestProviderAdapter(Func<ProviderLookupRequest, ProviderLookupResponse> lookup, string code, bool isEnabled)
        {
            _lookup = lookup;
            Descriptor = new ProviderDescriptor(
                code,
                $"{code} provider",
                isEnabled,
                OfficialSourceOnly: false,
                Precedence: DriverGuardian.ProviderAdapters.Abstractions.Models.ProviderPrecedence.PlatformVendor);
        }

        public ProviderDescriptor Descriptor { get; }

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => Task.FromResult(_lookup(request));
    }
}
