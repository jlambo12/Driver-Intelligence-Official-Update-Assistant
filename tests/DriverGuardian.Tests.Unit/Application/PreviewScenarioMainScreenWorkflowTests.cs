using DriverGuardian.Application.MainScreen;

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
        Assert.Contains(result.RecommendationDetails, detail => detail.ManualHandoffReady);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnPopulatedHistoryScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.PopulatedHistoryAndExport);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.NotEmpty(result.RecentHistory);
        Assert.NotEmpty(result.ReportExportPayload.PlainTextContent);
    }
}
