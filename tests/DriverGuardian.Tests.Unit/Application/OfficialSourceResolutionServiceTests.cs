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
    public async Task ResolveAsync_ShouldPickBestCandidate_ByTrustAndCompatibility()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new FakeProvider("oem", [BuildCandidate(SourceTrustLevel.OemSupportPortal, CompatibilityConfidence.High, null)]),
            new FakeProvider("publisher", [BuildCandidate(SourceTrustLevel.OfficialPublisherSite, CompatibilityConfidence.Medium, new Uri("https://vendor.com/driver.exe"))])
        };

        var service = new OfficialSourceResolutionService(providers);
        var result = await service.ResolveAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.NotNull(result.Candidate);
        Assert.Equal("publisher", result.Candidate.ProviderCode);
        Assert.Equal(OfficialSourceActionTarget.DirectDownloadPage, result.Candidate.ActionTarget);
    }

    [Fact]
    public async Task ResolveAsync_WhenProviderThrows_ShouldCaptureFailure_AndContinue()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new ThrowingProvider("broken"),
            new FakeProvider("healthy", [BuildCandidate(SourceTrustLevel.OemSupportPortal, CompatibilityConfidence.Medium, null)])
        };

        var service = new OfficialSourceResolutionService(providers);
        var result = await service.ResolveAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.Single(result.Failures);
        Assert.Equal("broken", result.Failures[0].ProviderCode);
        Assert.NotNull(result.Candidate);
        Assert.Equal("healthy", result.Candidate.ProviderCode);
    }

    [Fact]
    public async Task BuildAsync_WhenDifferentHostCdnIsAllowed_ShouldAllowDirectDownload()
    {
        var provider = new FakeProvider("catalog", [
            BuildCandidate(
                SourceTrustLevel.OperatingSystemCatalog,
                CompatibilityConfidence.High,
                new Uri("https://download.windowsupdate.com/file.cab"),
                sourceUri: new Uri("https://www.catalog.update.microsoft.com/Search.aspx?q=test"))]);

        var logger = new RecordingDiagnosticLogger();
        var actionService = new OfficialSourceActionService(
            new OfficialSourceResolutionService([provider]),
            new OpenOfficialSourceActionEvaluator(),
            logger);

        var action = await actionService.BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Equal(OfficialSourceActionTarget.DirectDownloadPage, action.ActionTarget);
        Assert.Equal("https://download.windowsupdate.com/file.cab", action.ApprovedOfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_WhenDownloadHostIsNotAllowlisted_ShouldFallbackToSourcePage()
    {
        var provider = new FakeProvider("oem", [
            BuildCandidate(
                SourceTrustLevel.OemSupportPortal,
                CompatibilityConfidence.High,
                new Uri("https://cdn.vendor.test/file.exe"),
                sourceUri: new Uri("https://support.vendor.test/drivers"))]);

        var logger = new RecordingDiagnosticLogger();
        var actionService = new OfficialSourceActionService(
            new OfficialSourceResolutionService([provider]),
            new OpenOfficialSourceActionEvaluator(),
            logger);

        var action = await actionService.BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Equal(OfficialSourceActionTarget.SourcePage, action.ActionTarget);
        Assert.Equal("https://support.vendor.test/drivers", action.ApprovedOfficialSourceUrl);
    }

    [Fact]
    public async Task BuildAsync_WhenProviderThrows_ShouldLogWarningAndKeepWorkflowAlive()
    {
        var providers = new IOfficialProviderAdapter[]
        {
            new ThrowingProvider("broken"),
            new FakeProvider("healthy", [BuildCandidate(SourceTrustLevel.OemSupportPortal, CompatibilityConfidence.Medium, null)])
        };

        var logger = new RecordingDiagnosticLogger();
        var actionService = new OfficialSourceActionService(
            new OfficialSourceResolutionService(providers),
            new OpenOfficialSourceActionEvaluator(),
            logger);

        var action = await actionService.BuildAsync(BuildDrivers(), BuildRecommendations(), CancellationToken.None);

        Assert.True(action.IsReady);
        Assert.Single(logger.WarningEvents);
        Assert.Contains("Provider=broken", logger.WarningEvents[0], StringComparison.Ordinal);
        Assert.Contains("exceptionType=InvalidOperationException", logger.WarningEvents[0], StringComparison.Ordinal);
    }

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
        Uri? downloadUri,
        Uri? sourceUri = null)
        => new(
            "id",
            "2.0.0",
            null,
            compatibility,
            new SourceEvidence(
                sourceUri ?? new Uri("https://vendor.com/support"),
                "Vendor",
                trustLevel,
                true,
                "evidence"),
            downloadUri);

    private sealed class FakeProvider(string code, IReadOnlyCollection<ProviderCandidate> candidates) : IOfficialProviderAdapter
    {
        public ProviderDescriptor Descriptor => new(code, code, true, true, ProviderPrecedence.PrimaryOem);

        public Task<ProviderLookupResponse> LookupAsync(ProviderLookupRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderLookupResponse(code, true, candidates, null));
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
