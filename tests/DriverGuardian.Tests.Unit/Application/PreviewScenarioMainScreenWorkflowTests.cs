using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class PreviewScenarioMainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnManualReadyScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationReadyForManualAction);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed, result.OfficialSourceAction.Resolution);
        Assert.True(result.OfficialSourceAction.IsReady);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnLimitedEvidenceScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationWithLimitedEvidence);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(OfficialSourceResolutionKind.InsufficientEvidence, result.OfficialSourceAction.Resolution);
        Assert.False(result.OfficialSourceAction.IsReady);
    }
}
