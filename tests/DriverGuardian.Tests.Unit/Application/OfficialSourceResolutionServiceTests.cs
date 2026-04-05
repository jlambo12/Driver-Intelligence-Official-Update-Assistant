using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.Domain.Devices;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;
using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class OfficialSourceResolutionServiceTests
{
    [Fact]
    public async Task BuildAsync_ConfirmedDirectOfficialPageOnSameHost_ShouldBeApprovedTarget()
    {
        var provider = new FakeProvider("publisher", [
            BuildCandidate(
                SourceTrustLevel.OfficialPublisherSite,
                CompatibilityConfidence.High,
                sourceUri: new Uri("https://drivers.dell.com/gpu/123"),
                downloadUri: new Uri("https://drivers.dell.com/files/gpu-123.exe"))]);

        var logger = new RecordingDiagnosticLogger();
        var actionService = BuildActionService([provider], logger);

        var action = await actionService.BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Equal(OfficialSourceActionTarget.DirectDownloadPage, action.ActionTarget);
        Assert.Equal("https://drivers.dell.com/gpu/123", action.ApprovedOfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_VendorSupportWithDifferentHostCdn_ShouldKeepSupportPageAsApprovedTarget()
    {
        var provider = new FakeProvider("oem", [
            BuildCandidate(
                SourceTrustLevel.OemSupportPortal,
                CompatibilityConfidence.High,
                sourceUri: new Uri("https://support.hp.com/device/abc"),
                downloadUri: new Uri("https://cdn.dell.com/file.cab"))]);

        var logger = new RecordingDiagnosticLogger();
        var actionService = BuildActionService([provider], logger);

        var action = await actionService.BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Equal(OfficialSourceActionTarget.SourcePage, action.ActionTarget);
        Assert.Equal("https://support.hp.com/device/abc", action.ApprovedOfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_UnknownTrustCandidate_ShouldReturnInsufficientEvidence()
    {
        var provider = new FakeProvider("weak", [
            BuildCandidate(
                SourceTrustLevel.Unknown,
                CompatibilityConfidence.High,
                sourceUri: new Uri("https://dell.com/unknown"),
                downloadUri: null)]);

        var action = await BuildActionService([provider], new RecordingDiagnosticLogger())
            .BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.False(action.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, action.ResolutionOutcome);
        Assert.Null(action.ApprovedOfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_OneProviderThrows_AnotherSucceeds_ShouldContinueAndLogFailure()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new ThrowingProvider("broken"),
            new FakeProvider("healthy", [
                BuildCandidate(
                    SourceTrustLevel.OemSupportPortal,
                    CompatibilityConfidence.Medium,
                    sourceUri: new Uri("https://support.hp.com/ok"),
                    downloadUri: null)])
        };

        var logger = new RecordingDiagnosticLogger();
        var action = await BuildActionService(providers, logger)
            .BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Single(logger.WarningEvents);
        Assert.Contains("Provider=broken", logger.WarningEvents[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_AllProvidersFailOrNoUsableResult_ShouldReturnInsufficientEvidence()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new ThrowingProvider("broken"),
            new FailureProvider("failed", "network timeout"),
            new FakeProvider("unknown", [
                BuildCandidate(
                    SourceTrustLevel.Unknown,
                    CompatibilityConfidence.High,
                    sourceUri: new Uri("https://dell.com/u"),
                    downloadUri: null)])
        };

        var action = await BuildActionService(providers, new RecordingDiagnosticLogger())
            .BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.False(action.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, action.ResolutionOutcome);
    }

    [Fact]
    public async Task ResolveAsync_StrongerConfirmedResult_ShouldBeatWeakerCandidate()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new FakeProvider("catalog", [
                BuildCandidate(
                    SourceTrustLevel.OperatingSystemCatalog,
                    CompatibilityConfidence.High,
                    sourceUri: new Uri("https://catalog.update.test/driver"),
                    downloadUri: null)]),
            new FakeProvider("publisher", [
                BuildCandidate(
                    SourceTrustLevel.OfficialPublisherSite,
                    CompatibilityConfidence.Medium,
                    sourceUri: new Uri("https://drivers.dell.com/driver/999"),
                    downloadUri: null)])
        };

        var resolved = await new OfficialSourceResolutionService(providers)
            .ResolveAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.NotNull(resolved.Candidate);
        Assert.Equal("publisher", resolved.Candidate.ProviderCode);
        Assert.Equal(OfficialSourceActionTarget.DirectDownloadPage, resolved.Candidate.ActionTarget);
    }

    private static OfficialSourceActionService BuildActionService(IEnumerable<IOfficialProviderAdapter> providers, RecordingDiagnosticLogger logger)
        => new(
            new OfficialSourceResolutionService(providers),
            new OpenOfficialSourceActionEvaluator(),
            logger);

    private static IReadOnlyCollection<InstalledDriverSnapshot> BuildDrivers()
        =>
        [
            new InstalledDriverSnapshot(
                new DeviceIdentity("DEV\\1"),
                new HardwareIdentifier("PCI\\VEN_1234&DEV_ABCD"),
                "1.0.0",
                null,
                "Vendor")
        ];

    private static IReadOnlyCollection<RecommendationSummary> BuildRecommendations()
        =>
        [
            new RecommendationSummary(new DeviceIdentity("DEV\\1"), true, "reason", "2.0.0")
        ];

    private static ProviderCandidate BuildCandidate(
        SourceTrustLevel trustLevel,
        CompatibilityConfidence compatibility,
        Uri sourceUri,
        Uri? downloadUri)
        => new(
            "id",
            "2.0.0",
            null,
            compatibility,
            compatibility == CompatibilityConfidence.High
                ? HardwareMatchQuality.ExactHardwareId
                : HardwareMatchQuality.NormalizedHardwareId,
            new SourceEvidence(sourceUri, "Vendor", trustLevel, true, "evidence"),
            downloadUri);

    private sealed class FakeProvider(string code, IReadOnlyCollection<ProviderCandidate> candidates) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(code, code, true, true, ProviderPrecedence.PrimaryOem);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderLookupResponse(code, true, candidates, null));
    }

    private sealed class FailureProvider(string code, string reason) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(code, code, true, true, ProviderPrecedence.PrimaryOem);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderLookupResponse(code, false, [], reason));
    }

    private sealed class ThrowingProvider(string code) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(code, code, true, true, ProviderPrecedence.PrimaryOem);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("provider failure");
    }

    private sealed class RecordingDiagnosticLogger : IDiagnosticLogger
    {
        public List<string> WarningEvents { get; } = [];

        public Task LogInfoAsync(string eventName, string message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task LogWarningAsync(string eventName, string message, CancellationToken cancellationToken)
        {
            WarningEvents.Add($"{eventName}:{message}");
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string eventName, string message, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
