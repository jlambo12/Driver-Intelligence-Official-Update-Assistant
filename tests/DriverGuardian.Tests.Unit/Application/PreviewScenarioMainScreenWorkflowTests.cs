using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class PreviewScenarioMainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnPlaceholderForFirstRunScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.FirstRunPreScan);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(Guid.Empty, result.ScanSessionId);
        Assert.Equal(0, result.RecommendedCount);
        Assert.Empty(result.RecommendationDetails);
        Assert.Empty(result.ReportExportPayload.PlainTextContent);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnManualReadyScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationReadyForManualAction);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(1, result.RecommendedCount);
        Assert.Equal(1, result.ManualHandoffReadyCount);
        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.Resolution);
        Assert.Contains(result.RecommendationDetails, detail => detail.ManualHandoffReady && detail.OfficialSourceResolution == OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnLimitedEvidenceScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationWithLimitedEvidence);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.False(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, result.OfficialSourceAction.Resolution);
        Assert.Contains(result.RecommendationDetails, detail => detail.OfficialSourceResolution == OfficialSourceResolutionKind.InsufficientEvidence);
    }
}
